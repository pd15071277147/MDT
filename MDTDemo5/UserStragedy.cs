namespace MDTDemo5
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.OleDb;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    public class UserStragedy
    {

        /// <summary>
        /// 整理类策略总表，默认配置表，UI表和默认UI表，整理配置策略UI表，默认配置表和输出表，整理执行策略UI表和默认配置表存入数据库
        /// </summary>
        /// <param name="username"></param>
        /// <param name="caseNo"></param>
        public void ClassStragedyIn(string username, string caseNo)
        {
            
            //UserStragedy stragedy = new UserStragedy();
            DatabaseProcessing processing = new DatabaseProcessing();
            //从源数据中取出所有1：N的类连线，存入表格cdt
            MySqlFhHelper Original = new MySqlFhHelper(processing.SqlOriginal);
            MySqlFhHelper Process = new MySqlFhHelper(processing.SqlProcess);
            string selectSqlCommand = "SELECT Name,Notes,SourceCard,DestCard,SourceRole,DestRole,Start_Object_ID,End_Object_ID FROM t_connector where SourceCard='1..*' or SourceCard='0..*'or DestCard='0..*'or DestCard='1..*';";
            DataTable cdt = Original.GetTable(selectSqlCommand);


            //增加列
            cdt.Columns.Add("id", typeof(string));
            cdt.Columns.Add("用例编号", typeof(string));
            cdt.Columns.Add("用户名", typeof(string));
            cdt.Columns.Add("Start_Object_Name", typeof(string));
            cdt.Columns.Add("Start_Object_Package", typeof(string));
            cdt.Columns.Add("End_Object_Name", typeof(string));
            cdt.Columns.Add("End_Object_Package", typeof(string));
            cdt.Columns.Add("Dut_Type", typeof(string));
            cdt.Columns.Add("Dut_Type_Detial", typeof(string));
            cdt.Columns.Add("Dut_Name", typeof(string));



            int cnr = cdt.Rows.Count;//记录总表行数
            //在过程数据中找出PIM4表和本用例不重复的活动图,查询筛选出PIM4中本用例用到的数据

            selectSqlCommand = "SELECT * from Sequencediagram";
            DataTable dtpim4 = Process.GetTable(selectSqlCommand);//PIM4总表
            int nrdp4 = dtpim4.Rows.Count;
            selectSqlCommand = "SELECT distinct sequencename from Acticitydiagram where usecaseobjectid='" + caseNo + "'";
            DataTable dtactive = Process.GetTable(selectSqlCommand);//用例活动图中不重复的活动图列表

            //遍历PIM4总表，当其在用例活动图中未出现时，删除该行
            for (int i = nrdp4 - 1; i >= 0; i--)
            {
                int num8 = 0;
                foreach (DataRow row2 in dtactive.Rows)
                {
                    if (row2["sequencename"].ToString().Trim() == dtpim4.Rows[i]["sequenceName"].ToString().Trim())
                    {
                        num8++;
                    }
                }
                if (num8 == 0)
                {
                    dtpim4.Rows.RemoveAt(i);
                }
            }
            nrdp4 = dtpim4.Rows.Count;//行数发生变化，重新读取行数

            //遍历1:N类连线总表,和用例PIM4相比，去除未用的类连线
            for (int j = cnr - 1; j >= 0; j--)
            {
                int num10 = 0;
                int num11 = 0;
                for (int num12 = 0; num12 < nrdp4; num12++)
                {
                    string str5 = cdt.Rows[j]["Start_Object_ID"].ToString().Trim();
                    string str6 = cdt.Rows[j]["End_Object_ID"].ToString().Trim();
                    string str7 = dtpim4.Rows[num12]["messagesenderobjectid"].ToString().Trim();
                    string str8 = dtpim4.Rows[num12]["messagereceiverobjectid"].ToString().Trim();
                    if ((str5 == str7) || (str5 == str8))
                    {
                        num10++;
                    }

                    if ((str6 == str7) || (str6 == str8))
                    {
                        num11++;
                    }

                }
                if ((num10 == 0) || (num11 == 0))
                {
                    cdt.Rows.RemoveAt(j);
                }
            }
            cnr = cdt.Rows.Count;//更新类连线总表行数
            //在源数据中根据类连线的ID查出类名和所在包ID
            //stragedy.MySqlFhHelper(processing.SqlOriginal);
            for (int k = 0; k < cnr; k++)
            {
                string str9 = cdt.Rows[k]["Start_Object_ID"].ToString();
                string str10 = cdt.Rows[k]["End_Object_ID"].ToString();
                selectSqlCommand = "SELECT Name,Package_ID FROM t_object where Object_ID=" + str9 + ";";
                DataTable table16 = Original.GetTable(selectSqlCommand);
                selectSqlCommand = "SELECT Name,Package_ID FROM t_object where Object_ID=" + str10 + ";";
                DataTable table17 = Original.GetTable(selectSqlCommand);
                cdt.Rows[k]["Start_Object_Name"] = table16.Rows[0]["Name"];
                cdt.Rows[k]["Start_Object_Package"] = table16.Rows[0]["Package_ID"];
                cdt.Rows[k]["End_Object_Name"] = table17.Rows[0]["Name"];
                cdt.Rows[k]["End_Object_Package"] = table17.Rows[0]["Package_ID"];
                cdt.Rows[k]["用例编号"] = caseNo;
                cdt.Rows[k]["用户名"] = username;
            }


            //取出PIM3的Package_ID，根据ID取出Instrument包ID
            string str2 = null;
            string str3 = null;
            selectSqlCommand = "SELECT Package_ID FROM t_package where Name='PIM-3';";
            DataTable table4 = Original.GetTable(selectSqlCommand);
            if (table4.Rows.Count <= 0)
            {
                throw new Exception("未取到PIM-3的Package_ID");
            }
            str2 = table4.Rows[0]["Package_ID"].ToString();
            selectSqlCommand = "SELECT Package_ID FROM t_package where Name='Instrument' and Parent_ID=" + str2 + ";";
            DataTable table5 = Original.GetTable(selectSqlCommand);
            if (table5.Rows.Count <= 0)
            {
                throw new Exception("未取到PIM-3下的Instrument的Package_ID");
            }


            //根据取出的Instrument的Package_ID，遍历类连线总表，判断类是设备还是仪表
            str3 = table5.Rows[0]["Package_ID"].ToString();
            for (int m = 0; m < cnr; m++)
            {
                if (cdt.Rows[m]["Start_Object_Package"].ToString() == str3)
                {
                    cdt.Rows[m]["Dut_Type"] = "Instrument";
                }
                else
                {
                    cdt.Rows[m]["Dut_Type"] = "Dut";
                }
            }


            //查询过程数据，查出本用例活动图中所有不重复的泳道名
            //stragedy.MySqlFhHelper(processing.SqlProcess);
            selectSqlCommand = "SELECT distinct SequenceObjectName from acticitydiagram where usecaseobjectid='" + caseNo + "'";
            DataTable table6 = Process.GetTable(selectSqlCommand);
            table6.Columns.Add("Dut_Type", typeof(string));
            table6.Columns.Add("Dut_Name", typeof(string));

            //查询源数据，取出用例mini拓扑名
            //stragedy.MySqlFhHelper(processing.sqlOriginal);
            selectSqlCommand = "SELECT Notes FROM t_diagram where Package_ID='" + caseNo + "';";
            string str4 = Original.GetTable(selectSqlCommand).Rows[0]["Notes"].ToString().Trim();
            //查出迷你拓扑相关三张表数据
            //stragedy.MySqlFhHelper(processing.SqlProcess);
            selectSqlCommand = "SELECT * FROM miniTopo where (nodeName like '%instrument_%' or nodeName like 'dut_%') and TopoName='" + str4 + "'; ";
            DataTable table8 = Process.GetTable(selectSqlCommand);//本用例minitopu表
            DataTable table9 = new DataTable();
            DataTable table10 = new DataTable();
            int num3 = table8.Rows.Count;
            if (num3 < 1)
            {
                throw new Exception("minitupo数据有误！");
            }
            //一次查出本用例所有设备的miniTopoConnect表
            selectSqlCommand = "SELECT * FROM miniTopoConnect where connListName='" + table8.Rows[0]["connListName"].ToString().Trim() + "' and (forceObj like '%instrument_%' or curObj like 'dut_%')";
            for (int n = 1; n < num3; n++)
            {
                string str12 = " union SELECT * FROM miniTopoConnect where connListName='" + table8.Rows[n]["connListName"].ToString().Trim() + "' and (forceObj like '%instrument_%' or curObj like 'dut_%')";
                selectSqlCommand = selectSqlCommand + str12;
            }
            table9 = Process.GetTable(selectSqlCommand);//本用例miniTopoConnect表
            //一次查出本用例所有设备的miniTopoCnctAttr表
            selectSqlCommand = "SELECT * FROM miniTopoCnctAttr where curobjAttr='" + table9.Rows[0]["curobjAttr"].ToString().Trim() + "' and (attrName = 'port_role' or attrName='dut_type') ";
            for (int num17 = 1; num17 < table9.Rows.Count; num17++)
            {
                string str13 = "union SELECT * FROM miniTopoCnctAttr where curobjAttr='" + table9.Rows[num17]["curobjAttr"].ToString().Trim() + "' and (attrName = 'port_role' or attrName='dut_type') ";
                selectSqlCommand = selectSqlCommand + str13;
            }
            table10 = Process.GetTable(selectSqlCommand);//本用例的miniTopoCnctAttr表
            int num15 = table6.Rows.Count;//本用例泳道名列表行数

            //取出泳道名，遍历本用例的miniTopoCnctAttr表的attrDef，如果相等，在判断attrName是port_role（则设备为Instrument）还是dut_type（设备是Dut），根据同行的curobjAttr查出设备名称
            for (int i = 0; i < num15; i++)
            {
                string str14 = table6.Rows[i]["SequenceObjectName"].ToString().Trim();
                foreach (DataRow row3 in table10.Rows)
                {
                    string str15 = row3["attrDef"].ToString().Trim();
                    if (str14 == str15)
                    {
                        if (row3["attrName"].ToString().Trim() == "port_role")
                        {
                            DataRow row4 = table6.NewRow();
                            row4["SequenceObjectName"] = table6.Rows[i]["SequenceObjectName"];
                            row4["Dut_Type"] = "Instrument";
                            foreach (DataRow row5 in table9.Rows)
                            {
                                if (row5["curobjAttr"].ToString().Trim() == row3["curobjAttr"].ToString().Trim())
                                {
                                    row4["Dut_Name"] = row5["forceObj"].ToString().Trim();
                                }
                            }
                            table6.Rows.Add(row4);
                        }
                        else if (row3["attrName"].ToString().Trim() == "dut_type")
                        {
                            DataRow row6 = table6.NewRow();
                            row6["SequenceObjectName"] = table6.Rows[i]["SequenceObjectName"];
                            row6["Dut_Type"] = "Dut";
                            foreach (DataRow row7 in table9.Rows)
                            {
                                if (row7["curobjAttr"].ToString().Trim() == row3["curobjAttr"].ToString().Trim())
                                {
                                    row6["Dut_Name"] = row7["curObj"].ToString().Trim();
                                }
                            }
                            table6.Rows.Add(row6);
                        }
                    }
                }
            }

            //删除泳道名表中的空白行
            for (int num19 = table6.Rows.Count - 1; num19 >= 0; num19--)
            {
                if (table6.Rows[num19]["Dut_Name"].ToString().Trim() == "")
                {
                    table6.Rows.Remove(table6.Rows[num19]);
                }
            }

            //删除泳道名表中的重复行
            string[] columnNames = new string[] { "SequenceObjectName", "Dut_Type", "Dut_Name" };
            table6 = table6.DefaultView.ToTable(true, columnNames);

            //用泳道名表和类连线总表相比较设备类型，添加泳道名和设备名称
            int num4 = table6.Rows.Count;
            for (int i = cnr - 1; i >= 0; i--)
            {
                int num21 = 0;
                for (int j = 0; j < num4; j++)
                {
                    string str16 = cdt.Rows[i]["Dut_Type"].ToString();
                    string str17 = table6.Rows[j]["Dut_Type"].ToString();
                    string str18 = table6.Rows[j]["SequenceObjectName"].ToString();
                    string str19 = table6.Rows[j]["Dut_Name"].ToString();
                    if (str16 == str17)
                    {
                        if (num21 == 0)
                        {
                            cdt.Rows[i]["Dut_Type_Detial"] = str18;
                            cdt.Rows[i]["Dut_Name"] = str19;
                            num21++;
                        }
                        else
                        {
                            cdt.Rows.Add(cdt.Rows[i].ItemArray);
                            cnr = cdt.Rows.Count;
                            cdt.Rows[cnr - 1]["Dut_Type_Detial"] = str18;
                            cdt.Rows[cnr - 1]["Dut_Name"] = str19;
                        }
                    }
                }
            }

            //建立类连线总表主键值
            cnr = cdt.Rows.Count;
            for (int i = 0; i < cnr; i++)
            {
                object[] objArray1 = new object[] { username, "_", caseNo, "_", i };
                cdt.Rows[i]["id"] = string.Concat(objArray1);
            }

            //整理格式，删除不必要的列
            cdt.TableName = "类策略总表";
            cdt.Columns.Remove("Start_Object_ID");
            cdt.Columns.Remove("End_Object_ID");
            cdt.Columns.Remove("Start_Object_Package");
            cdt.Columns.Remove("End_Object_Package");
            DataSet ds = new DataSet();

            //从总表中取出几列组成UI表
            string[] textArray2 = new string[] { "id", "用户名", "用例编号", "Dut_Type_Detial", "Name", "Notes", "Dut_Name" };
            DataTable cdtUI = cdt.DefaultView.ToTable(false, textArray2);
            cdtUI.TableName = "类策略UI";
            cdtUI.Columns.Add("Value", typeof(string));
            ds.Tables.Add(cdtUI);
            ds.Tables.Add(cdt);
            //
            //
            //
            //
            //
            //
            //
            //




            //整理配置策略表


            //在过程数据中查出用例活动图表
            //stragedy.MySqlFhHelper(processing.SqlProcess);
            selectSqlCommand = "SELECT * from acticitydiagram where usecaseobjectid='" + caseNo + "';";
            DataTable dtact = Process.GetTable(selectSqlCommand);
            dtact.Columns.Add("参数", typeof(string));
            dtact.Columns.Add("值", typeof(string));
            dtact.Columns.Add("作用类", typeof(string));
            dtact.Columns.Add("用户名", typeof(string));
            dtact.Columns.Add("id", typeof(string));
            dtact.Columns.Add("用例编号", typeof(string));
            dtact.Columns.Add("序号", typeof(string));
            dtact.Columns.Add("设备类型", typeof(string));
            dtact.Columns.Add("步骤名称", typeof(string));
            dtact.Columns.Add("设备名称", typeof(string));
            //查询PIM4表中有约束的数据
            selectSqlCommand = "SELECT * from sequencediagram where sequenceConstraint != ''; ";
            DataTable table14 = Process.GetTable(selectSqlCommand);
            int nract = dtact.Rows.Count;//用例活动图行数
            int max = 0;
            //遍历用例活动图与约束数据表相比较，解析并添加约束配置
            DataRow row = dtact.NewRow();
            for (int i = nract - 1; i >= 0; i--)
            {
                foreach (DataRow row8 in table14.Rows)
                {
                    if (dtact.Rows[i]["sequencename"].ToString().Trim() == row8["sequenceName"].ToString().Trim())
                    {
                        string str20 = row8["sequenceConstraint"].ToString().Trim();
                        string str21 = str20.Replace("&&", "");
                        int count = ((str20.Length - str21.Length) / 2) + 1;
                        if (count > max)
                        {
                            max = count;
                        }
                        string input = row8["sequenceConstraint"].ToString().Trim();
                        if ((count - 1) == 0)
                        {
                            char[] separator = new char[] { '=' };
                            string[] strArray = input.Split(separator);
                            row = dtact.NewRow();
                            row["参数"] = strArray[0];
                            row["值"] = strArray[strArray.Length - 1];
                            row["作用类"] = row8["messagereceiver"].ToString().Trim();
                            row["用户名"] = username;
                            row["usecaseobjectid"] = dtact.Rows[i]["usecaseobjectid"].ToString().Trim();
                            row["SequenceObjectName"] = dtact.Rows[i]["SequenceObjectName"].ToString().Trim();
                            row["seqno"] = dtact.Rows[i]["seqno"].ToString().Trim();
                            row["sequencename"] = dtact.Rows[i]["sequencename"].ToString().Trim();
                            row["用例编号"] = dtact.Rows[i]["usecaseobjectid"].ToString().Trim();
                            row["序号"] = dtact.Rows[i]["seqno"].ToString().Trim();
                            row["设备类型"] = dtact.Rows[i]["SequenceObjectName"].ToString().Trim();
                            row["步骤名称"] = dtact.Rows[i]["sequencename"].ToString().Trim();
                            dtact.Rows.Add(row);
                        }
                        else
                        {
                            string[] strArray2 = Regex.Split(input, "&&", RegexOptions.IgnoreCase);
                            char[] chArray2 = new char[] { '=' };
                            string[] strArray3 = strArray2[strArray2.Length - 1].Split(chArray2);
                            row = dtact.NewRow();
                            row["参数"] = strArray3[0];
                            row["值"] = strArray3[strArray3.Length - 1];
                            row["作用类"] = row8["messagereceiver"].ToString().Trim();
                            row["用户名"] = username;
                            row["usecaseobjectid"] = dtact.Rows[i]["usecaseobjectid"].ToString().Trim();
                            row["SequenceObjectName"] = dtact.Rows[i]["SequenceObjectName"].ToString().Trim();
                            row["seqno"] = dtact.Rows[i]["seqno"].ToString().Trim();
                            row["sequencename"] = dtact.Rows[i]["sequencename"].ToString().Trim();
                            row["用例编号"] = dtact.Rows[i]["usecaseobjectid"].ToString().Trim();
                            row["序号"] = dtact.Rows[i]["seqno"].ToString().Trim();
                            row["设备类型"] = dtact.Rows[i]["SequenceObjectName"].ToString().Trim();
                            row["步骤名称"] = dtact.Rows[i]["sequencename"].ToString().Trim();
                            dtact.Rows.Add(row);
                        }
                    }
                }
                dtact.Rows.RemoveAt(i);
            }
            //和泳道名表相比，添加mini拓扑的设备名称
            num4 = table6.Rows.Count;
            for (int i = dtact.Rows.Count - 1; i >= 0; i--)
            {
                string str23 = dtact.Rows[i]["设备类型"].ToString().Trim();
                for (int j = 0; j < num4; j++)
                {
                    string str24 = table6.Rows[j]["SequenceObjectName"].ToString().Trim();
                    string str25 = table6.Rows[j]["Dut_Name"].ToString().Trim();
                    if (str23 == str24)
                    {
                        DataRow row9 = dtact.NewRow();
                        for (int k = 0; k < dtact.Rows[i].Table.Columns.Count; k++)
                        {
                            row9[k] = dtact.Rows[i][k];
                        }
                        row9["设备名称"] = str25;
                        dtact.Rows.Add(row9);
                    }
                }
            }

            //添加主键值，删除空白行
            for (int i = dtact.Rows.Count - 1; i >= 0; i--)
            {
                object[] objArray2 = new object[] { username, "_", caseNo, "_", i };
                dtact.Rows[i]["id"] = string.Concat(objArray2);
                if (dtact.Rows[i]["设备名称"].ToString().Trim() == "")
                {
                    dtact.Rows.Remove(dtact.Rows[i]);
                }
            }

            //调整表格式
            dtact.TableName = "配置策略UI";
            dtact.Columns.Remove("usecaseobjectid");
            dtact.Columns.Remove("SequenceObjectName");
            dtact.Columns.Remove("seqno");
            dtact.Columns.Remove("sequencename");
            dtact.Columns.Remove("usecaseName");
            dtact.Columns.Remove("serial_Numbe");
            dtact.Columns["参数"].SetOrdinal(7);
            dtact.Columns["值"].SetOrdinal(8);
            dtact.Columns["作用类"].SetOrdinal(9);
            dtact.Columns["用户名"].SetOrdinal(1);
            dtact.Columns["id"].SetOrdinal(0);
            dtact.Columns["用例编号"].SetOrdinal(2);
            dtact.Columns["序号"].SetOrdinal(5);
            dtact.Columns["设备类型"].SetOrdinal(3);
            dtact.Columns["步骤名称"].SetOrdinal(6);
            dtact.Columns["设备名称"].SetOrdinal(4);
            ds.Tables.Add(dtact);
            DataTable dtactUI = new DataTable();
            dtactUI = dtact.Copy();
            dtactUI.TableName = "配置策略输出";



            //执行策略数据整理
            DataTable dtaction = new DataTable
            {
                TableName = "执行策略"
            };
            dtaction.Columns.Add("id", typeof(string));
            dtaction.Columns.Add("用户名", typeof(string));
            dtaction.Columns.Add("用例编号", typeof(string));
            dtaction.Columns.Add("参数遍历", typeof(string));
            dtaction.Columns.Add("单步执行", typeof(string));
            dtaction.Columns.Add("判断出错后，重新判断次数", typeof(string));
            dtaction.Columns.Add("重新判断间隔时间（s）", typeof(string));
            dtaction.Columns.Add("循环起点泳道", typeof(string));
            dtaction.Columns.Add("循环起点序号", typeof(string));
            dtaction.Columns.Add("循环起点活动图", typeof(string));
            dtaction.Columns.Add("循环终点泳道", typeof(string));
            dtaction.Columns.Add("循环终点序号", typeof(string));
            dtaction.Columns.Add("循环终点活动图", typeof(string));
            dtaction.Columns.Add("循环次数", typeof(string));
            DataRow r = dtaction.NewRow();
            r["用户名"] = username;
            r["用例编号"] = caseNo;
            r["id"] = username + "_" + caseNo + "_0";
            dtaction.Rows.Add(r);
            ds.Tables.Add(dtaction);




            SaveDefaultData(ds, username, caseNo);
            ds.Tables.Add(dtactUI);
            SaveTempData(ds, username, caseNo);
        }

        public void ClassStragedyOut(string username, string caseNo)
        {
            //UserStragedy stragedy = new UserStragedy();
            DatabaseProcessing processing = new DatabaseProcessing();
            MySqlFhHelper Original = new MySqlFhHelper(processing.SqlOriginal);
            MySqlFhHelper Process = new MySqlFhHelper(processing.SqlProcess);
            //stragedy.SqlHelper(processing.SqlProcess);
            string[] textArray1 = new string[] { "SELECT * FROM 类策略UI where 用户名='", username, "' and 用例编号='", caseNo, "'; " };
            string selectSqlCommand = string.Concat(textArray1);
            DataTable table = Process.GetTable(selectSqlCommand);
            string[] textArray2 = new string[] { "SELECT * FROM 类策略总表 where 用户名='", username, "' and 用例编号='", caseNo, "'; " };
            selectSqlCommand = string.Concat(textArray2);
            DataTable table2 = Process.GetTable(selectSqlCommand);
            int count = table2.Rows.Count;
            foreach (DataRow row in table.Rows)
            {
                foreach (DataRow row2 in table2.Rows)
                {
                    string str2 = row["Dut_Type_Detial"].ToString().Trim();
                    string str3 = row2["Dut_Type_Detial"].ToString().Trim();
                    string str4 = row["Name"].ToString().Trim();
                    string str5 = row2["Name"].ToString().Trim();
                    string str6 = row["Notes"].ToString().Trim();
                    string str7 = row2["Notes"].ToString().Trim();
                    if (((str2 == str3) && (str4 == str5)) && (str6 == str7))
                    {
                        if (row2["SourceCard"].ToString() != "1")
                        {
                            row2["SourceCard"] = row["Value"];
                        }
                        else
                        {
                            row2["DestCard"] = row["Value"];
                        }
                    }
                }
            }
            DataSet ds = new DataSet();
            DataTable table3 = new DataTable
            {
                TableName = "类策略输出"
            };
            table3.Columns.Add("泳道名", typeof(string));
            table3.Columns.Add("id", typeof(string));
            table3.Columns.Add("用户名", typeof(string));
            table3.Columns.Add("用例编号", typeof(string));
            string[] columnNames = new string[] { "Dut_Type_Detial" };
            DataTable table4 = table2.DefaultView.ToTable(false, columnNames);
            foreach (DataRow row3 in table4.Rows)
            {
                DataRow row4 = table3.NewRow();
                row4["泳道名"] = row3["Dut_Type_Detial"];
                table3.Rows.Add(row4);
            }
            for (int i = 0; i < count; i++)
            {
                string str8 = table2.Rows[i]["Dut_Type_Detial"].ToString().Trim();
                string str9 = table2.Rows[i]["Start_Object_Name"].ToString().Trim();
                string str10 = table2.Rows[i]["SourceRole"].ToString().Trim();
                string str11 = table2.Rows[i]["End_Object_Name"].ToString().Trim();
                string str12 = table2.Rows[i]["DestRole"].ToString().Trim();
                string name = str9 + "_" + str10;
                string str14 = str11 + "_" + str12;
                string str15 = table2.Rows[i]["SourceCard"].ToString();
                string str16 = table2.Rows[i]["DestCard"].ToString();
                foreach (DataRow row5 in table3.Rows)
                {
                    if (row5["泳道名"].ToString().Trim() == str8)
                    {
                        if (table3.Columns.Contains(name))
                        {
                            row5[name] = str15;
                        }
                        else
                        {
                            table3.Columns.Add(new DataColumn(name, typeof(string)));
                            row5[name] = str15;
                        }
                        if (table3.Columns.Contains(str14))
                        {
                            row5[str14] = str16;
                        }
                        else
                        {
                            table3.Columns.Add(new DataColumn(str14, typeof(string)));
                            row5[str14] = str16;
                        }
                    }
                }
            }
            int num2 = 0;
            foreach (DataRow row6 in table3.Rows)
            {
                row6["用户名"] = table2.Rows[0]["用户名"];
                row6["用例编号"] = table2.Rows[0]["用例编号"];
                object[] objArray1 = new object[] { row6["用户名"].ToString().Trim(), "_", row6["用例编号"].ToString().Trim(), "_", num2 };
                row6["id"] = string.Concat(objArray1);
                num2++;
            }
            ds.Tables.Add(table3);
            SaveTempData(ds, username, caseNo);
        }


        public static void SaveDefaultData(DataSet ds, string username, string caseNo)
        {
            //UserStragedy stragedy = new UserStragedy();
            DatabaseProcessing processing = new DatabaseProcessing();
            //MySqlConnection connection = new MySqlConnection(processing.SqlProcess);
            MySqlFhHelper Process = new MySqlFhHelper(processing.SqlProcess);
            // MySqlCommand command = new MySqlCommand();
            //SqlCnt.Open();
            
            //MySqlCommand command = connection.CreateCommand();
            foreach (DataTable table in ds.Tables)
            {
                string str = "";
                string str2 = "";
                string str3 = "";
                string sqlcommand = "select * from " + table.TableName + "_default;";
                if (Process.GetTable(sqlcommand) == null)
                {
                    int count = table.Rows.Count;
                    int num2 = table.Columns.Count;
                    string[] textArray1 = new string[] { "CREATE TABLE ", table.TableName, "_default (", table.Columns[0].ColumnName.ToString(), " varchar(255)," };
                    str = string.Concat(textArray1);
                    str2 = table.Columns[0].ColumnName.ToString() + ",";
                    for (int i = 1; i < (num2 - 1); i++)
                    {
                        str = str + table.Columns[i].ColumnName.ToString() + " varchar(255),";
                        str2 = str2 + table.Columns[i].ColumnName.ToString() + ",";
                    }
                    str = str + table.Columns[num2 - 1].ColumnName.ToString() + " varchar(255))";
                    str2 = str2 + table.Columns[num2 - 1].ColumnName.ToString();
                    sqlcommand = str;
                    Process.ExecuteSqlCommand(sqlcommand,false);
                    for (int j = 0; j < count; j++)
                    {
                        str3 = (" INSERT INTO " + table.TableName + "_default (" + str2 + ") VALUES ( '") + table.Rows[j][0].ToString() + "','";
                        for (int k = 1; k < (num2 - 1); k++)
                        {
                            str3 = str3 + table.Rows[j][k].ToString() + "','";
                        }
                        str3 = str3 + table.Rows[j][num2 - 1].ToString() + "')";
                        sqlcommand = str3;
                        Process.ExecuteSqlCommand(sqlcommand, false);
                    }
                    str = "alter table " + table.TableName + "_default add primary key (id);";
                    sqlcommand = str;
                    Process.ExecuteSqlCommand(sqlcommand, false);
                }
                else
                {
                    //stragedy.SqlHelper(processing.SqlProcess);
                    string[] textArray3 = new string[] { "select * from ", table.TableName, "_default where 用户名 = '", username, "' and 用例编号 = '", caseNo, "'" };
                    string selectSqlCommand = string.Concat(textArray3);
                    DataTable table2 = Process.GetTable(selectSqlCommand);
                    int num6 = table2.Rows.Count;
                    if (num6 == 0)
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            table2.Rows.Add(row.ItemArray);
                        }
                    }
                    else
                    {
                        for (int m = 0; m < num6; m++)
                        {
                            table2.Rows[m].Delete();
                        }
                        foreach (DataRow row2 in table.Rows)
                        {
                            table2.Rows.Add(row2.ItemArray);
                        }
                    }
                    Process.UpdateTable(table2.GetChanges(), selectSqlCommand);
                }
            }
            Process.CloseConnection();

        }

        public static void SaveTempData(DataSet ds, string username, string caseNo)
        {
            //UserStragedy stragedy = new UserStragedy();
            DatabaseProcessing processing = new DatabaseProcessing();
            //MySqlConnection connection = new MySqlConnection(processing.SqlProcess);
            MySqlFhHelper Process = new MySqlFhHelper(processing.SqlProcess);
            //MySqlCommand command = new MySqlCommand();
            //connection.Open();
            //command = connection.CreateCommand();
            foreach (DataTable table in ds.Tables)
            {
                string str = "";
                string str2 = "";
                string str3 = "";
                string sqlcommand = "select * from " + table.TableName + ";";
                if (Process.GetTable(sqlcommand) == null)
                {
                    int count = table.Rows.Count;
                    int num2 = table.Columns.Count;
                    string[] textArray1 = new string[] { "CREATE TABLE ", table.TableName, "(", table.Columns[0].ColumnName.ToString(), " varchar(255)," };
                    str = string.Concat(textArray1);
                    str2 = table.Columns[0].ColumnName.ToString() + ",";
                    for (int i = 1; i < (num2 - 1); i++)
                    {
                        str = str + table.Columns[i].ColumnName.ToString() + " varchar(255),";
                        str2 = str2 + table.Columns[i].ColumnName.ToString() + ",";
                    }
                    str = str + table.Columns[num2 - 1].ColumnName.ToString() + " varchar(255))";
                    str2 = str2 + table.Columns[num2 - 1].ColumnName.ToString();
                    sqlcommand = str;
                    Process.ExecuteSqlCommand(sqlcommand,false);
                    for (int j = 0; j < count; j++)
                    {
                        str3 = (" INSERT INTO " + table.TableName + "(" + str2 + ") VALUES ( '") + table.Rows[j][0].ToString() + "','";
                        for (int k = 1; k < (num2 - 1); k++)
                        {
                            str3 = str3 + table.Rows[j][k].ToString() + "','";
                        }
                        str3 = str3 + table.Rows[j][num2 - 1].ToString() + "')";
                        sqlcommand = str3;
                        Process.ExecuteSqlCommand(sqlcommand,false);
                    }
                    str = "alter table " + table.TableName + " add primary key (id);";
                    sqlcommand = str;
                    Process.ExecuteSqlCommand(sqlcommand,false);
                }
                else
                {
                    //stragedy.SqlHelper(processing.SqlProcess);
                    string[] textArray3 = new string[] { "select * from ", table.TableName, " where 用户名 = '", username, "' and 用例编号 = '", caseNo, "'" };
                    string selectSqlCommand = string.Concat(textArray3);
                    DataTable table2 = Process.GetTable(selectSqlCommand);
                    int num6 = table2.Rows.Count;
                    if (num6 == 0)
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            table2.Rows.Add(row.ItemArray);
                        }
                    }
                    else
                    {
                        for (int m = 0; m < num6; m++)
                        {
                            table2.Rows[m].Delete();
                        }
                        foreach (DataRow row2 in table.Rows)
                        {
                            table2.Rows.Add(row2.ItemArray);
                        }
                    }
                    Process.UpdateTable(table2.GetChanges(), selectSqlCommand);
                }
            }
            Process.CloseConnection();
        }

        
    }
}

