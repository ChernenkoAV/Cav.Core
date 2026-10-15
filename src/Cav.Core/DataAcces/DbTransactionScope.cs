#pragma warning disable CA1003 // Используйте экземпляры обработчика универсальных событий

using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace Cav;

/// <summary>
/// Делегат окончания работы транзакции
/// </summary>
/// <param name="connName">Имя соединения с БД</param>
public delegate void DbTransactionScopeEnd(string connName);

/// <summary>
/// "Груповая" транзакция. Обертка для вызовов в БД. Только для одного DbConnection
/// </summary>
public sealed class DbTransactionScope : IDisposable
{
    private bool complete;
    private string connName;
    private static AsyncLocal<Guid?> rootTran = new();
    private static AsyncLocal<bool> needRolback = new() { Value = false };

    private static ConcurrentDictionary<string, DbTransaction> transactions = [];

    private readonly Guid currentTran;
    private readonly bool isRootTran;

    private static string getKeyTran(string? conName) =>
        $"{conName ?? DbContext.defaultNameConnection}|{rootTran.Value ?? Guid.NewGuid()}";

    /// <summary>
    /// Создание нового экземпляра обертки транзации
    /// </summary>
    /// <param name="connectionName">Имя соедиенения, для которого назначается транзакция</param>
    public DbTransactionScope(string? connectionName = null)
    {
        using var beginTranActivity = Activity.Current?.Source.StartNewActivity("transaction begin");
        currentTran = Guid.NewGuid();

        beginTranActivity?.AddTag("transaction.id", currentTran);

        connName = connectionName ??= DbContext.defaultNameConnection;

        rootTran.Value ??= currentTran;

        isRootTran = rootTran.Value == currentTran;

        beginTranActivity?.AddTag("transaction.kind", isRootTran ? "root" : "nested");

        if (!isRootTran)
            return;

        if (!transactions.TryGetValue(getKeyTran(connName), out var transaction))
        {
            transactions.AddOrUpdate(getKeyTran(connName), DbContext.Connection(connName).BeginTransaction(), (k, t) => t);
            beginTranActivity?.AddTag("transaction.state", "create");
        }
    }

    internal static DbTransaction? TransactionGet(string? connectionName = null)
    {
        transactions.TryGetValue(getKeyTran(connectionName), out var res);

        return res != null && res.Connection == null
            ? throw new InvalidOperationException("TransactionGet Несогласованное состояние объекта транзакции. Соедиение с БД сброшено.")
            : res;
    }

    #region Члены IDisposable

    /// <summary>
    /// Пометить, что транзакцию можно закомитить
    /// </summary>
    public void Complete() => complete = true;

    /// <summary>
    /// Реализация IDisposable
    /// </summary>
    public void Dispose()
    {
        using var endTranActivity = Activity.Current?.Source.StartNewActivity("transaction end");

        endTranActivity?.AddTag("transaction.id", currentTran.ToString());
        endTranActivity?.AddTag("transaction.kind", isRootTran ? "root" : "nested");
        endTranActivity?.AddTag("transaction.complete", complete);

        //если эту (а эта может быть корневой или какая-то вложенная)
        // не пометили, что она ок, то надо ролбэчить
        if (!complete)
            needRolback.Value = true;

        // если это не корневая транзанкция - выходим.
        if (!isRootTran)
            return;

        transactions.TryRemove(getKeyTran(connName), out var tran);
        var connection = tran.Connection;
        rootTran.Value = null;

        try
        {
            if (tran is null)
            {
                endTranActivity?.AddEvent(new("tran is null"));
                endTranActivity?.SetStatus(ActivityStatusCode.Error);
                return;
            }

            if (connection is null)
            {
                endTranActivity?.AddEvent(new("connection is null"));
                endTranActivity?.SetStatus(ActivityStatusCode.Error);
                return;
            }

            if (needRolback.Value)
            {
                // ролбэчим
                endTranActivity?.AddTag("transaction.state", "rollback");

                tran.Rollback();

                TransactionRollback?.Invoke(connName);
            }
            else
            {
                // коммитим
                endTranActivity?.AddTag("transaction.state", "commit");

                tran.Commit();

                TransactionCommit?.Invoke(connName);
            }
        }
        catch
        {
            endTranActivity?.AddEvent(new("set state error"));
            endTranActivity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            tran?.Dispose();
            connection?.Dispose();
            tran = null;
        }
    }

    #endregion

    /// <summary>
    /// Событие окончания транзакции комитом
    /// </summary>
    public static event DbTransactionScopeEnd TransactionCommit = null!;

    /// <summary>
    /// Событие окончание транзакции откатом
    /// </summary>
    public static event DbTransactionScopeEnd TransactionRollback = null!;
}
