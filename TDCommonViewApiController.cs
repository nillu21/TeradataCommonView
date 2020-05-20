using Api.Models.TDCommonView;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TestTool.Utility;

namespace Api.Controllers.TDCommonView
{
    [RoutePrefix("api/tdcommonview")]
    public class TDCommonViewController : ApiController
    {
        // Get initial databases created by system database owner(systemdatabaseowner has been replaced due to security promises)
        [HttpGet]
        [Route("database")]
        public IHttpActionResult GetDatabases()
        {
            string query = @"SELECT	o1.DataBaseName, Tables 
                FROM(
                    SELECT   DataBaseName, COUNT(*) AS Tables
                    FROM       DBC.TablesV
                    WHERE     TableKind = 'T'
                        AND
                        DataBaseName <> 'DBC'
                    GROUP BY DataBaseName) as o1
                INNER JOIN
                (
                    SELECT OwnerName, DatabaseName,
                                DBKind AS DB1, DBKind
                            FROM    dbc.DatabasesV
                            WHERE   DataBaseName<> 'DBC'

                                AND OwnerName = 'systemdatabaseowner') AS o2
                            on o1.DataBaseName = o2.DataBaseName";
            using (DbConnection conn = DbUtility.Connect())
            using (DbCommand sql = DbUtility.GenerateSQL(conn, query))
            using (var reader = sql.ExecuteReader())
            {
                List<TDDatabase> databases = new List<TDDatabase>();
                while (reader.Read())
                {
                    TDDatabase db = new TDDatabase
                    {
                        Name = reader.GetString(reader.GetOrdinal("DataBaseName")),
                        IsOpen = false,
                        Tables = new List<TDTable>()
                    };

                    databases.Add(db);
                };

                return Ok(databases);
            }

        }
        [HttpGet]
        [Route("tables")]
        public IHttpActionResult GetTables(string database)
        {
            string query = @"SELECT o2.DataBaseName,TableName FROM(SELECT OwnerName,DatabaseName,DBKind AS DB1,DBKind 
            FROM dbc.DatabasesV
             WHERE DataBaseName<> 'DBC' AND OwnerName = 'systemdatabaseowners'
                AND DataBaseName = '" + database + @"') AS o1
            INNER JOIN
                (SELECT* FROM dbc.tablesV) AS o2
                on o1.DataBaseName = o2.DataBaseName
            ORDER BY 1";

            using (DbConnection conn = DbUtility.Connect())
            using (DbCommand sql = DbUtility.GenerateSQL(conn, query))
            using (var reader = sql.ExecuteReader())

            {
                List<TDTable> tables = new List<TDTable>();
                while (reader.Read())
                {
                    TDTable table = new TDTable
                    {
                        Name = reader.GetString(reader.GetOrdinal("TableName")),
                        ColumnCount = 0,
                        Database = database
                    };

                    tables.Add(table);
                };
                return Ok(tables);
            }
        }

        
        [HttpGet]
        [Route("tablespace")]
        public IHttpActionResult GetTableSpace(string database, string table)
        {
            string query =
                @"Lock Dbc.TableSizeV for Access 
                SELECT	'' (Title ''),S.TableName AS Name,
		                CASE TableKind 
			                WHEN 'O' THEN 'T' 
			                WHEN 'E' THEN 'P' 
			                WHEN 'A' THEN 'F' 
			                WHEN 'S' THEN 'F' 
			                WHEN 'R' THEN 'F' 
			                WHEN 'B' THEN 'F' 
			                ELSE TableKind 
		                END AS " + ((char)34)  + @"Type" + ((char)34) + @",SUM(CurrentPerm) AS CurrentPerm,SUM(PeakPerm) AS PeakPerm,
                        (100 - (AVG(CurrentPerm) / NULLIFZERO(MAX(CurrentPerm)) * 100)) AS SkewFactor,

                        ''(Title ''),CreatorName,CommentString
                FROM    Dbc.TableSizeV S, dbc.TablesV T
                WHERE S.TableName = T.TableName

                    AND S.DataBaseName = T.DataBaseName

                    AND S.DataBaseName = '" + database + @"' 

                    AND S.TableName = '" + table + @"' 
                GROUP BY 2,3,8,9
                ORDER BY 3,2; ";

            using (DbConnection conn = DbUtility.Connect())
            using (DbCommand sql = DbUtility.GenerateSQL(conn, query))
            using (var reader = sql.ExecuteReader())
            {
                List<String> tableSpace = new List<string>();
                while (reader.Read())
                {
                    tableSpace.Add(reader.GetString(reader.GetOrdinal("CurrentPerm")));
                    tableSpace.Add(reader.GetString(reader.GetOrdinal("PeakPerm")));
                    tableSpace.Add(reader.GetString(reader.GetOrdinal("SkewFactor")));
                    break;
                };

                return Ok(tableSpace);
            }

        }
        [HttpGet]
        [Route("tablerights")]
        public IHttpActionResult GetRights(string database, string table)
        {
            string query = @"WITH Parms (DbName,TblName) AS (
	                    SELECT	'" + database + "', '" + table + @"') 
                    SELECT	UserName,ColumnName,AccessRight,GrantAuthority,GrantorName,
		                    AllnessFlag 
                    FROM	dbc.AllRightsV,Parms 
                    WHERE	DatabaseName=Parms.DbName 
	                    AND TableName=Parms.TblName 
                    UNION 
                    SELECT	RoleName,ColumnName,AccessRight,'R',GrantorName,'' 
                    FROM	dbc.AllRoleRightsV,Parms 
                    WHERE	DatabaseName=Parms.DbName 
	                    AND TableName=Parms.TblName 
                    ORDER BY 1,2,3;";


            
            using (DbConnection conn = DbUtility.Connect())
            using (DbCommand sql = DbUtility.GenerateSQL(conn, query))
            using (var reader = sql.ExecuteReader())
            {
                List<TDRights> tableRights = new List<TDRights>();
                while (reader.Read())
                {
                    TDRights right = new TDRights
                    {
                        Username = reader.GetString(reader.GetOrdinal("UserName")),
                        RightType = reader.GetString(reader.GetOrdinal("AccessRight")),
                        GrantorName = reader.GetString(reader.GetOrdinal("GrantorName"))
                    };

                    tableRights.Add(right);
                    
                };

               

                return Ok(tableRights);
            }



        }
        
        [HttpGet]
        [Route("tablereferences")]
        public IHttpActionResult GetReferences(string database, string table)
        {
            string query = @"WITH P (DbName,TblName) AS (
	            SELECT	'" + database + "','" + table + @"') 
            SELECT	DatabaseName,TVMName,TableKind AS " + ((char)34) + @"Type" + ((char)34) + @" 
            FROM dbc.TVM T, dbc.dbase D, P
            WHERE D.DatabaseId = T.DatabaseId
                AND CreateText LIKE '%" + ((char)34) + @"' || P.DbName || '" + ((char)34) + @"." + ((char)34) + @"' || P.TblName || '" + ((char)34) + @"%'(NOT CS)
            UNION
            SELECT  DatabaseName,TVMName,TableKind AS " + ((char)34) + @"Type" + ((char)34) + @"
            FROM dbc.TextTbl X, dbc.dbase D, dbc.TVM T, P
            WHERE X.TextType = 'C'

                AND X.TextString LIKE '%" + ((char)34) + @"' || P.DbName || '" + ((char)34) + @"." + ((char)34) + @"' || P.TblName || '" + ((char)34) + @"%'(NOT CS)

                AND X.DatabaseId = D.DatabaseId

                AND X.TextId = T.TVMId
            UNION
            SELECT  ChildDB,ChildTable,'T'
            FROM dbc.RI_Distinct_Children,P
            WHERE   ParentDB = P.DbName

                AND ParentTable = P.TblName
            MINUS
            SELECT  DatabaseName,TVMName,TableKind
            FROM    dbc.TVM T, dbc.dbase D, P
            WHERE D.DatabaseId = T.DatabaseId

                AND DatabaseName = P.DbName

                AND TVMName = P.TblName
            ORDER BY 1,2 ";
            using(DbConnection conn = DbUtility.Connect())
            using (DbCommand sql = DbUtility.GenerateSQL(conn, query))
            using (var reader = sql.ExecuteReader())
            {
                List<TDReference> tableReferences = new List<TDReference>();
                while (reader.Read())
                {
                    
                    TDReference reference = new TDReference
                    {
                        DataBaseName = reader.GetString(reader.GetOrdinal("DataBaseName")),
                        TVMName = reader.GetString(reader.GetOrdinal("TVMName")),
                        Type = reader.GetString(reader.GetOrdinal("Type"))
                    };
                    Console.WriteLine(reference);
                    tableReferences.Add(reference);
                
                };
                return Ok(tableReferences);
            }

        }
        
    }
}
