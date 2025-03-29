using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;

namespace Cav.DataAcces;

/// <summary>
/// Базовый класс адаптера получения и изменения данных в БД.
/// </summary>
/// <typeparam name="TRow">Класс, на который производится отражение данных из БД</typeparam>
/// <typeparam name="TSelectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
/// <typeparam name="TUpdateParams">Клас, типизирующий параметры адаптера на изменение</typeparam>
/// <typeparam name="TDeleteParams">Клас, типизирующий параметры адаптера на удаление</typeparam>
public class DataAccesBase<TRow, TSelectParams, TUpdateParams, TDeleteParams> : DataAccesBase<TRow, TSelectParams>, IDataAcces<TRow, TSelectParams, TUpdateParams, TDeleteParams>
    where TRow : class, new()
    where TSelectParams : IAdapterParametrs
    where TUpdateParams : IAdapterParametrs
    where TDeleteParams : IAdapterParametrs
{
    #region Add

    /// <summary>
    /// Добавить объект в БД
    /// </summary>
    /// <param name="newObj">Экземпляр объекта, который необходимо добавит в БД</param>
    public async Task AddAsync(TRow newObj, CancellationToken cancellationToken = default)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Insert, insertExpression, newObj);
        if (insertPropKeyFieldMap.Any())
        {
            using var resExec = await FillTableAsync(execCom, cancellationToken).ConfigureAwait(false);
            foreach (var dbrow in resExec.Rows.Cast<DataRow>())
                foreach (var ff in insertPropKeyFieldMap)
                    ff.Value(newObj, dbrow);
        }
        else
            await ExecuteNonQueryAsync(execCom, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Добавить объект в БД
    /// </summary>
    /// <param name="newObj">Экземпляр объекта, который необходимо добавит в БД</param>
    public void Add(TRow newObj)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Insert, insertExpression, newObj);
        if (insertPropKeyFieldMap.Any())
        {
            using var resExec = FillTable(execCom);
            foreach (var dbrow in resExec.Rows.Cast<DataRow>())
                foreach (var ff in insertPropKeyFieldMap)
                    ff.Value(newObj, dbrow);
        }
        else
            ExecuteNonQuery(execCom);
    }

    private ConcurrentDictionary<string, Action<TRow, DataRow>> insertPropKeyFieldMap = new();
    private LambdaExpression? insertExpression;

    /// <summary>
    /// Сопоставление свойства объекта отражения и имени параметра адаптера
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="property">Свойство</param>
    /// <param name="paramName">Имя параметра</param>
    /// <param name="typeParam">Тип параметра в БД</param>
    /// <param name="addedConvertFunct">Дополнительная функция преобразования</param> 
    protected void MapInsertParam<T>(
        Expression<Func<TRow, T>> property,
        string paramName,
        DbType? typeParam = null,
        Expression<Func<T, object?>>? addedConvertFunct = null)
    {
        MapParam<T>(CommandActionType.Insert, property, paramName, typeParam, addedConvertFunct);

        var paramExp = property.Parameters[0];

        var insadeCall = insertExpression != null
            ? ParameterRebinder.ReplaceParameters(property, insertExpression)
            : paramExp;

        Expression metodCall =
                Expression.Call(
                    typeof(HeplerDataAcces),
                    nameof(HeplerDataAcces.ForParceParams),
                    [typeof(TRow)],
                    insadeCall,
                    Expression.Quote(property),
                    Expression.Quote(property));

        insertExpression = Expression.Lambda<Action<TRow>>(
            metodCall,
            paramExp);
    }

    /// <summary>
    /// Сопоставление полей объекта отражения данных и возврата ключей послк операции вставки данных
    /// </summary>
    /// <param name="property"></param>
    /// <param name="fieldName"></param>
    protected void MapInsertKeyParam<T>(Expression<Func<TRow, T>> property, string fieldName) =>
        MapSelectFieldInDictionary(insertPropKeyFieldMap, property, fieldName);

    #endregion

    #region Delete

    /// <summary>
    /// Удаление по предикату 
    /// </summary>
    /// <param name="deleteParams"></param>
    public async Task DeleteAsync(
        Expression<Action<TDeleteParams>> deleteParams,
        CancellationToken cancellationToken = default)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Delete, deleteParams);
        await ExecuteNonQueryAsync(execCom, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Удаление по предикату 
    /// </summary>
    /// <param name="deleteParams"></param>
    public void Delete(Expression<Action<TDeleteParams>> deleteParams)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Delete, deleteParams);
        ExecuteNonQuery(execCom);
    }

    /// <summary>
    /// Сопоставление объекта параметров удаления и параметров адаптера удаления
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="property">Свойство</param>
    /// <param name="paramName">Имя параметра</param>
    /// <param name="typeParam">Тип параметра в БД</param>
    /// <param name="addedConvertFunct">Дополнительная функция преобразования</param>
    protected void MapDeleteParam<T>(
        Expression<Func<TDeleteParams, T>> property,
        string paramName,
        DbType? typeParam = null,
        Expression<Func<T, object?>>? addedConvertFunct = null) =>
        MapParam<T>(CommandActionType.Delete, property, paramName, typeParam, addedConvertFunct);

    #endregion

    #region Update

    /// <summary>
    /// Обновление данных
    /// </summary>
    /// <param name="updateParams"></param>
    public async Task UpdateAsync(
        Expression<Action<TUpdateParams>> updateParams,
        CancellationToken cancellationToken = default)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Update, updateParams);
        await ExecuteNonQueryAsync(execCom, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Обновление данных
    /// </summary>
    /// <param name="updateParams"></param>
    public void Update(Expression<Action<TUpdateParams>> updateParams)
    {
        Configured();

        var execCom = addParamToCommand(CommandActionType.Update, updateParams);
        ExecuteNonQuery(execCom);
    }

    /// <summary>
    /// Сопоставление свойств класса параметров обновления и параметров адаптера
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="property">Свойство</param>
    /// <param name="paramName">Имя параметра</param>
    /// <param name="typeParam">Тип параметра в БД</param>
    /// <param name="addedConvertFunct">Дополнительная функция преобразования</param>
    protected void MapUpdateParam<T>(
        Expression<Func<TUpdateParams, T>> property,
        string paramName,
        DbType? typeParam = null,
        Expression<Func<T, object?>>? addedConvertFunct = null) =>
        MapParam<T>(CommandActionType.Update, property, paramName, typeParam, addedConvertFunct);

    #endregion

}
