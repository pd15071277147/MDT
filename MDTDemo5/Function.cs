using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace MDTDemo5
{
    class Function
    {
        /// <summary>
        /// 打印数据表
        /// </summary>
        /// <param name="table">要打印的DataTable表</param>
        public static void PrintTable(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    Console.Write(row[column] + "\t");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 将datarow[] 转换成 datatable
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static DataTable ToDataTable(DataRow[] rows)
        {
            if (rows == null || rows.Length == 0) return null;
            DataTable tmp = rows[0].Table.Clone(); // 复制DataRow的表结构
            foreach (DataRow row in rows)
            {
                tmp.ImportRow(row); // 将DataRow添加到DataTable中
            }
            return tmp;
        }

        /// <summary>
        /// 打印dataset的内容，包括表单名称
        /// </summary>
        /// <param name="dataSet"></param>
        public static void PrintValuesDataSet(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                Console.WriteLine("TableName: " + table.TableName);
                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        Console.Write("\t" + row[column]);
                    }
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// 将dataset中的表格保存为本地excel表格
        /// </summary>
        /// <param name="ds"></param>
        //public static void ExportDataSetToExcel(DataSet ds, string fileName)
        //{
        //    if (File.Exists(fileName))
        //    {
        //        File.Delete(fileName);
        //    }

        //    Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
        //    excelApp.Visible = false;
        //    // Microsoft.Office.Interop.Excel.Workbook excelWorkBook = excelApp.Workbooks.Open(fileName);
        //    Microsoft.Office.Interop.Excel.Workbook excelWorkBook = excelApp.Workbooks.Add();

        //    foreach (DataTable table in ds.Tables)
        //    {
        //        Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet = excelWorkBook.Sheets.Add();
        //        excelWorkSheet.Name = table.TableName;  //sheet名为datatable表名

        //        for (int i = 1; i < table.Columns.Count + 1; i++)
        //        {
        //            excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
        //        }

        //        for (int j = 0; j < table.Rows.Count; j++)
        //        {
        //            for (int k = 0; k < table.Columns.Count; k++)
        //            {
        //                excelWorkSheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();
        //            }
        //        }

        //        //这里可以添加Sheet的格式代码

        //    }
        //    excelWorkBook.SaveAs(fileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing);
        //    //excelWorkBook.SaveAs(fileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing);
        //    excelWorkBook.Close(false, Type.Missing, Type.Missing);

        //    //excelWorkBook.Save();
        //    //excelWorkBook.Close();
        //    excelApp.Quit();
        //}

        /// <summary>
        /// 根据datatable获得列名
        /// </summary>
        /// <param name="dt">表对象</param>
        /// <returns>返回结果的数据列数组</returns>
        public static string[] GetColumnsByDataTable(DataTable dt)
        {
            string[] strColumns = null;
            if (dt.Columns.Count > 0)
            {
                int columnNum = 0;
                columnNum = dt.Columns.Count;
                strColumns = new string[columnNum];
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    strColumns[i] = dt.Columns[i].ColumnName;
                }
            }
            return strColumns;
        }

        /// <summary>
        ///将DataTable转换为标准的CSV
        /// </summary>
        /// <param name="table">数据表</param>
        /// <returns>返回标准的CSV</returns>
        public static string DataTableToCsv(DataTable table)
        {
            //以半角逗号（即,）作分隔符，列为空也要表达其存在。
            //列内容如存在半角逗号（即,）则用半角引号（即""）将该字段值包含起来。
            //列内容如存在半角引号（即"）则应替换成半角双引号（""）转义，并用半角引号（即""）将该字段值包含起来。
            StringBuilder sb = new StringBuilder();
            DataColumn colum;
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    colum = table.Columns[i];
                    if (i != 0) sb.Append(",");
                    if (colum.DataType == typeof(string) && row[colum].ToString().Contains(","))
                    {
                        sb.Append("\"" + row[colum].ToString().Replace("\"", "\"\"") + "\"");
                    }
                    else sb.Append(row[colum].ToString());
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将分析出来的topo表格写入保存为excel表格
        /// </summary>
        /// <param name="xmlFilePath"></param>
        public static void TopoTableWriteToExcel(string xmlFilePath)
        {
            string filePath = Path.GetDirectoryName(xmlFilePath);
            string fileName = Path.GetFileNameWithoutExtension(xmlFilePath);
            string xlsxFilePath = Path.Combine(filePath, fileName + ".xlsx");
            Console.WriteLine("xlsxFilePath:" + xlsxFilePath);
            //ExportDataSetToExcel(XmlTopoSort(xmlFilePath), xlsxFilePath);
        }

        /// <summary>
        /// 去掉表格中重复的行
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="filedNames"></param>
        /// <returns></returns>
        public static DataTable DistinctTable(DataTable dt, string[] filedNames)
        {
            DataView dv = dt.DefaultView;
            DataTable DistTable = dv.ToTable(dt.TableName, true, filedNames);
            return DistTable;
        }

    }
}
