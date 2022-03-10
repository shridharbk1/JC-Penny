using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using System.Data.Common;
using System.Reflection;

// ReSharper disable CoVariantArrayConversion

namespace IXP.App.DAL
{
    /// <summary>
    /// Database context interface (for unit tests).
    /// </summary>
    public interface IDataSource : IDisposable
    {
        #region Methods

        /// <summary>
        /// Executes a stored procedure that does not return any results.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The task.</returns>
        Task<int> ExecuteNonQuery(string procedureName, params SqlParameter[] parameters);

        Task<int> ExecuteNonQueryWithRollback(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets a single result from the database.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The result.</returns>
        Task<T> GetResult<T>(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets a list of results from the database.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The results.</returns>
        Task<List<T>> GetResults<T>(string procedureName, params SqlParameter[] parameters);

        Task<List<object>> GetResults(Type t, string procedureName, params SqlParameter[] parameters);

        Task<List<T>> GetResultsFromStoredFunction<T>(string functionName, params SqlParameter[] parameters);

        Task<T> GetResultFromStoredScalarFunction<T>(string functionName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets a list of dynamic objects from the database.
        /// </summary>
        /// <param name="procedureName">The procedure name.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The list of dynamics.</returns>
        Task<List<dynamic>> GetDynamicResults(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets a list of dynamic objects from the database.
        /// </summary>
        /// <param name="procedureName">The procedure name.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The list of dynamics.</returns>
        Task<List<dynamic>> GetDynamicResultsExtendedTimeOut(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets the data reader for the specified stored procedure.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The <see cref="SqlDataReader"/>.</returns>
        Task<SqlDataReader> GetDataReader(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Gets the data reader for the specified stored procedure.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The <see cref="SqlDataReader"/>.</returns>
        SqlDataReader GetDataReaderSync(string procedureName, params SqlParameter[] parameters);

        /// <summary>
        /// Creates a parameter for calling a stored procedure.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <param name="sqlDbType">The SQL database type.</param>
        /// <returns>The created SQL Parameter.</returns>
        SqlParameter CreateParameter(string parameterName, object parameterValue, SqlDbType? sqlDbType = null);

        /// <summary>
        /// Maps the results from the reader to a list of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects being returned from the stored procedure.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        List<T> MapResults<T>(SqlDataReader reader);

        /// <summary>
        /// Maps the results from the reader to a list of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects being returned from the stored procedure.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        T MapResult<T>(SqlDataReader reader);

        /// <summary>
        /// Maps the results from the reader to a list of dynamic type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        List<dynamic> MapDynamicResults(SqlDataReader reader);

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns></returns>
        Task<DbTransaction> BeginTransaction();

        #endregion Methods
    }

    /// <summary>
    /// Represents the SAM database.
    /// </summary>
    public sealed class DataSource : DbContext, IDataSource
    {
        #region Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private ILog logger;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSource"/> class.
        /// </summary>
        public DataSource()
            : base("IXPConnection")
        {
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;

            // Database.CommandTimeout = 120;
            Database.CommandTimeout = string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get("TimeOut"))? 180: Convert.ToInt32(ConfigurationManager.AppSettings.Get("TimeOut"));

            Database.Connection.Open();

            XmlConfigurator.Configure();

            logger = LogManager.GetLogger("DataSource");
        }

        #endregion Constructors

        #region Public Methods

        public async Task<int> ExecuteNonQueryWithRollback(string procedureName, params SqlParameter[] parameters)
        {
            procedureName = ConcatProcedureName(procedureName, parameters);
            DbContextTransaction tx = null;
            try
            {
                logger.DebugFormat("{0} - Starting call", procedureName);
                tx = Database.BeginTransaction();
                var result = await Database.ExecuteSqlCommandAsync(procedureName, parameters);
                tx.Commit();
                logger.DebugFormat("{0} - Finished call - {1} rows affected", procedureName, result);

                return result;
            }
            catch (Exception ex)
            {
                if (tx != null)
                {
                    tx.Rollback();
                }
                logger.Error($"{procedureName} - Error during call", ex);

                throw;
            }
        }

        /// <summary>
        /// Executes a stored procedure that does not return any results.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The task.</returns>
        public async Task<int> ExecuteNonQuery(string procedureName, params SqlParameter[] parameters)
        {
            procedureName = ConcatProcedureName(procedureName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", procedureName);
                var result = await Database.ExecuteSqlCommandAsync(procedureName, parameters);
                logger.DebugFormat("{0} - Finished call - {1} rows affected", procedureName, result);

                return result;
            }
            catch (Exception ex)
            {
                logger.Error($"{procedureName} - Error during call", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets a single result from the database.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The result.</returns>
        public async Task<T> GetResult<T>(string procedureName, params SqlParameter[] parameters)
        {
            procedureName = ConcatProcedureName(procedureName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", procedureName);
                var result = await Database.SqlQuery<T>(procedureName, parameters).FirstOrDefaultAsync();
                logger.DebugFormat("{0} - Finished call - record was {1}", procedureName, result == null ? "null" : "found");

                return result;
            }
            catch (Exception ex)
            {
                // Let the caller throw the error, they may be retrying when an APPLOCK can't be acquired.
                logger.DebugFormat($"{procedureName} - Error during call", ex);
                throw;
            }
        }

        public async Task<List<object>> GetResults(Type t, string procedureName, params SqlParameter[] parameters)
        {
            procedureName = ConcatProcedureName(procedureName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", procedureName);
                var results = await Database.SqlQuery(t, procedureName, parameters).ToListAsync();
                logger.DebugFormat("{0} - Finished call - {1} results", procedureName, results.Count);

                return results;
            }
            catch (Exception ex)
            {
                // Let the caller throw the error, they may be retrying when an APPLOCK can't be acquired.
                logger.DebugFormat($"{procedureName} - First-chance exception during call", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of results from the database.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The results.</returns>
        public async Task<List<T>> GetResults<T>(string procedureName, params SqlParameter[] parameters)
        {
            procedureName = ConcatProcedureName(procedureName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", procedureName);
                var results = await Database.SqlQuery<T>(procedureName, parameters).ToListAsync();
                logger.DebugFormat("{0} - Finished call - {1} results", procedureName, results.Count);

                return results;
            }
            catch (Exception ex)
            {
                // Let the caller throw the error, they may be retrying when an APPLOCK can't be acquired.
                logger.DebugFormat($"{procedureName} - First-chance exception during call", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of results from the database.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="functionName">The stored function name.</param>
        /// <param name="parameters">The parameters to the function.</param>
        /// <returns>The results.</returns>
        public async Task<List<T>> GetResultsFromStoredFunction<T>(string functionName, params SqlParameter[] parameters)
        {
            functionName = ConcatFunctionName(functionName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", functionName);
                var results = await Database.SqlQuery<T>(functionName, parameters).ToListAsync();
                logger.DebugFormat("{0} - Finished call - {1} results", functionName, results.Count);

                return results;
            }
            catch (Exception ex)
            {
                // Let the caller throw the error, they may be retrying when an APPLOCK can't be acquired.
                logger.DebugFormat($"{functionName} - First-chance exception during call", ex);
                throw;
            }
        }

        public async Task<T> GetResultFromStoredScalarFunction<T>(string functionName, params SqlParameter[] parameters)
        {
            functionName = ConcatScalarFunctionName(functionName, parameters);

            try
            {
                logger.DebugFormat("{0} - Starting call", functionName);
                var result = await Database.SqlQuery<T>(functionName, parameters).FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                // Let the caller throw the error, they may be retrying when an APPLOCK can't be acquired.
                logger.DebugFormat($"{functionName} - First-chance exception during call", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of dynamic objects from the database.
        /// </summary>
        /// <param name="procedureName">The procedure name.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The list of dynamics.</returns>
        public async Task<List<dynamic>> GetDynamicResultsExtendedTimeOut(string procedureName, params SqlParameter[] parameters)
        {
            using (var reader = await GetDataReaderExtendedTimeOut(procedureName, parameters))
            {
                return FillListFromReader(reader);
            }
        }

        /// <summary>
        /// Gets a list of dynamic objects from the database.
        /// </summary>
        /// <param name="procedureName">The procedure name.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The list of dynamics.</returns>
        public async Task<List<dynamic>> GetDynamicResults(string procedureName, params SqlParameter[] parameters)
        {
            using (var reader = await GetDataReader(procedureName, parameters))
            {
                return FillListFromReader(reader);
            }
        }

        /// <summary>
        /// Gets the data reader for the specified stored procedure.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The <see cref="SqlDataReader"/>.</returns>
        [ExcludeFromCodeCoverage]
        public async Task<SqlDataReader> GetDataReaderExtendedTimeOut(string procedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (var cmd = (SqlCommand)Database.Connection.CreateCommand())
                {
                    if (ConfigurationManager.AppSettings.Get("TimeOut") != null)
                    {
                        cmd.CommandTimeout = Convert.ToInt32(ConfigurationManager.AppSettings.Get("TimeOut"));
                    }

                    cmd.CommandText = procedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);

                    logger.DebugFormat("{0} - Starting call", procedureName);
                    var result = await cmd.ExecuteReaderAsync();
                    logger.DebugFormat("{0} - Finished call", procedureName);

                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{procedureName} - Error during call", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets the data reader for the specified stored procedure.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The <see cref="SqlDataReader"/>.</returns>
        [ExcludeFromCodeCoverage]
        public async Task<SqlDataReader> GetDataReader(string procedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (var cmd = (SqlCommand)Database.Connection.CreateCommand())
                {
                    if (Database.CommandTimeout.HasValue)
                    {
                        cmd.CommandTimeout = Database.CommandTimeout.Value;
                    }

                    cmd.CommandText = procedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    logger.DebugFormat("{0} - Starting call", procedureName);
                    var result = await cmd.ExecuteReaderAsync();
                    logger.DebugFormat("{0} - Finished call", procedureName);

                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{procedureName} - Error during call", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets the data reader for the specified stored procedure.
        /// </summary>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="parameters">The parameters to the procedure.</param>
        /// <returns>The <see cref="SqlDataReader"/>.</returns>
        [ExcludeFromCodeCoverage]
        public SqlDataReader GetDataReaderSync(string procedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (var cmd = (SqlCommand)Database.Connection.CreateCommand())
                {
                    cmd.CommandText = procedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);

                    logger.DebugFormat("{0} - Starting call", procedureName);
                    var reader = cmd.ExecuteReader();
                    logger.DebugFormat("{0} - Finished call", procedureName);

                    return reader;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{procedureName} - Error during call", ex);

                throw;
            }
        }

        /// <summary>
        /// Creates a parameter for calling a stored procedure.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <param name="sqlDbType">The SQL database type.</param>
        /// <returns>The created SQL Parameter.</returns>
        public SqlParameter CreateParameter(string parameterName, object parameterValue, SqlDbType? sqlDbType = null)
        {
            var param = new SqlParameter(parameterName, parameterValue ?? DBNull.Value);

            if (sqlDbType.HasValue)
            {
                param.SqlDbType = sqlDbType.Value;
            }

            return param;
        }

        /// <summary>
        /// Maps the results from the reader to a list of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects being returned from the stored procedure.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        public List<T> MapResults<T>(SqlDataReader reader)
        {
            var adapter = (IObjectContextAdapter)this;

            var results = adapter.ObjectContext.Translate<T>(reader).ToList();

            reader.NextResult();

            return results;
        }

        /// <summary>
        /// Maps the results from the reader to a list of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects being returned from the stored procedure.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        public T MapResult<T>(SqlDataReader reader)
        {
            var adapter = (IObjectContextAdapter)this;

            var results = adapter.ObjectContext.Translate<T>(reader).FirstOrDefault();

            reader.NextResult();

            return results;
        }

        /// <summary>
        /// Maps the results from the reader to a list of dynamic type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The mapped list.</returns>
        public List<dynamic> MapDynamicResults(SqlDataReader reader)
        {
            return FillListFromReader(reader);
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns></returns>
        public async Task<DbTransaction> BeginTransaction()
        {
            string methodInfo = MethodBase.GetCurrentMethod().DeclaringType.FullName;
            this.logger.DebugFormat("Start {0}", methodInfo);
            try
            {
                return await Task.FromResult(this.Database.BeginTransaction().UnderlyingTransaction);
            }
            catch (Exception ex)
            {
                this.logger.Error($"Exception occured in {methodInfo}", ex);
                throw;
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Disposes of the object.
        /// </summary>
        /// <param name="disposing">
        /// True to release both managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            logger = null;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Concatenates the procedure name and the parameter names.
        /// </summary>
        /// <param name="procedureName">The procedure name.</param>
        /// <param name="parameters">The SQL Parameters.</param>
        /// <returns>The concatenated procedure name.</returns>
        private string ConcatProcedureName(string procedureName, IEnumerable<SqlParameter> parameters)
        {
            return
                $"{procedureName} {string.Join(", ", parameters.Where(x => x != null).Select(t => GetParameterName(t)))}";
        }

        private string ConcatFunctionName(string procedureName, IEnumerable<SqlParameter> parameters)
        {
            return
                $"SELECT * FROM {procedureName}({string.Join(", ", parameters.Where(x => x != null).Select(t => "@" + t.ParameterName))})";
        }

        private string ConcatScalarFunctionName(string procedureName, IEnumerable<SqlParameter> parameters)
        {
            return
                $"SELECT {procedureName}({string.Join(", ", parameters.Where(x => x != null).Select(t => "@" + t.ParameterName))})";
        }

        /// <summary>
        /// Gets all column names from the data reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The list of columns names.</returns>
        private List<string> GetColumnNamesFromReader(SqlDataReader reader)
        {
            var list = new List<string>();
            var schemaTable = reader.GetSchemaTable();

            Debug.Assert(schemaTable != null, nameof(schemaTable) + " != null");
            for (int i = 0; i <= schemaTable.Rows.Count - 1; i++)
            {
                var dataRow = schemaTable.Rows[i];
                list.Add(dataRow["ColumnName"].ToString());
            }

            return list;
        }

        /// <summary>
        /// Fills a list of dynamics from the data reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The list of objects.</returns>
        private List<dynamic> FillListFromReader(SqlDataReader reader)
        {
            var columns = GetColumnNamesFromReader(reader);
            var list = new List<dynamic>();

            while (reader.Read())
            {
                IDictionary<string, object> dynamicObject = new ExpandoObject();

                for (var i = 0; i < columns.Count; ++i)
                {
                    dynamicObject[columns[i]] = reader.GetValue(i);
                }

                list.Add(dynamicObject);
            }

            return list;
        }

        private string GetParameterName(SqlParameter param)
        {
            if (param.Direction == ParameterDirection.Output)
                return "@" + param.ParameterName + " out";
            return "@" + param.ParameterName;
        }

        #endregion Private Methods
    }
}