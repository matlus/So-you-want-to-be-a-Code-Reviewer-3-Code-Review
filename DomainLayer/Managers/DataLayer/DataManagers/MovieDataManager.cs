using DomainLayer.Managers.Exceptions;
using DomainLayer.Managers.Models;
using DomainLayer.Managers.Parsers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DomainLayer.Managers.DataLayer.DataManagers
{
    internal sealed class MovieDataManager
    {
        private readonly SqlClientFactory sqlClientFactory = SqlClientFactory.Instance;

        private DbConnection CreateDbConnection()
        {
            var dbConnection = sqlClientFactory.CreateConnection();
            dbConnection.ConnectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=MovieDb;Integrated Security=True;TrustServerCertificate=True;";
            return dbConnection;
        }

        public async Task CreateMovie(Movie movie)
        {
            var dbConnection = CreateDbConnection();
            DbCommand dbCommand = null;
            DbTransaction dbTransaction = null;
            try
            {
                await dbConnection.OpenAsync().ConfigureAwait(false);
                dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);
                dbCommand = dbConnection.CreateCommand();
                dbCommand.Transaction = dbTransaction;

                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandText = "dbo.CreateMovie";

                AddDbParameter(dbCommand, "@Title", movie.Title, DbType.String, 50);
                AddDbParameter(dbCommand, "@Genre", GenreParser.ToString(movie.Genre), DbType.String, 50);
                AddDbParameter(dbCommand, "@Year", movie.Year, DbType.Int32, 0);
                AddDbParameter(dbCommand, "@ImageUrl", movie.ImageUrl, DbType.String, 200);

                await dbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                dbTransaction.Commit();
            }
            catch (DbException e)
            {
                dbTransaction?.Rollback();

                if (e.Message.Contains("duplicate key row in object 'dbo.Movie'", StringComparison.OrdinalIgnoreCase))
                {
                    throw new DuplicateMovieException($"A Movie with the Title: {movie.Title} already exists. Please use a different title", e);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                dbCommand?.Dispose();
                dbTransaction?.Dispose();
                dbConnection?.Dispose();
            }
        }

        private static void AddDbParameter(DbCommand dbCommand, string parameterName, object value, DbType dbType, int size)
        {
            var dbParameter = dbCommand.CreateParameter();
            dbParameter.ParameterName = parameterName;
            dbParameter.Value = value;
            dbParameter.DbType = dbType;
            dbParameter.Size = size;
            dbCommand.Parameters.Add(dbParameter);
        }

        public async Task<IEnumerable<Movie>> GetAllMovies()
        {
            using (var dbConnection = CreateDbConnection())
            {
                await dbConnection.OpenAsync().ConfigureAwait(false);
                using (var dbCommand = dbConnection.CreateCommand())
                {
                    dbCommand.CommandType = CommandType.StoredProcedure;
                    dbCommand.CommandText = "dbo.GetAllMovies";
                    var dbDataReader = await dbCommand.ExecuteReaderAsync().ConfigureAwait(false);
                    return MapToMovies(dbDataReader);
                }
            }
        }

        private static IEnumerable<Movie> MapToMovies(DbDataReader dbDataReader)
        {
            var movies = new List<Movie>();

            while (dbDataReader.Read())
            {
                movies.Add(new Movie(
                    title: (string)dbDataReader[0],
                    genre: GenreParser.Parse((string)dbDataReader[1]),
                    year: (int)dbDataReader[2],
                    imageUrl: (string)dbDataReader[3]));
            }

            return movies;
        }
    }
}
