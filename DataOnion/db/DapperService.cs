using System.Data.Common;
using Dapper;

namespace DataOnion.db
{
    public class DapperService<TDbConnection> : IDapperService<TDbConnection>
        where TDbConnection : DbConnection
    {
        private readonly TDbConnection _connection;

        public DapperService(
            TDbConnection connection
        )
        {
            _connection = connection;
        }

        public async Task<IEnumerable<TReturn>> QueryAndReturnAsync<TReturn>(
            string query,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {
                var result = await conn.QueryAsync<TReturn>(
                    query,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond>(
            string query,
            Func<TFirst, TSecond, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird>(
            string query,
            Func<TFirst, TSecond, TThird, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TThird, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<IEnumerable<TFirst>> QueryAndReturnAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(
            string query,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TFirst> mappingFunction,
            string? splitOn = null,
            object? parameters = null
        )
        {
            using (var conn = _connection)
            {                
                var result = await conn.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TFirst>(
                    query,
                    mappingFunction,
                    parameters
                );

                return result;
            }
        }

        public async Task<int> ExecuteAsync(
            string query,
            object? parameters
        )
        {
            using (var conn = _connection)
            {
                return await conn.ExecuteAsync(query, parameters);
            }
        }
    }
}