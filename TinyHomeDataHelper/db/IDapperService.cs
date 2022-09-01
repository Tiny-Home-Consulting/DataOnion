using System.Data.Common;

namespace TinyHomeDataHelper.db
{
    public interface IDapperService<TDbConnection>
        where TDbConnection : DbConnection
    {
        Task<IEnumerable<TReturn>> QueryAndReturnAsync<TReturn>(
            string query,
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond>(
            string query,
            Func<TFirst, TSecond, TFirst> mappingFunction,
            string? splitOn = null, 
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird>(
            string query,
            Func<TFirst, TSecond, TThird, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        );

        Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        );

        Task<int> ExecuteAsync(
            string query,
            object? parameters = null
        );
    }
}