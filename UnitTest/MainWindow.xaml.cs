using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WsUtils.SqliteEFUtils;

namespace UnitTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string dbPath = "Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "/UnitTest.db";
            var conn = new SQLiteConnection(dbPath);//创建数据库实例，指定文件位置  
            conn.Open();//打开数据库，若文件不存在会自动创建
            //string sql = "CREATE TABLE IF NOT EXISTS __migrationHistory (MigrationId TEXT PRIMARY KEY,ContextKey TEXT NOT NULL,ProductVersion TEXT NOT NULL ,Model TEXT NOT NULL); ";
            string sql = "CREATE TABLE IF NOT EXISTS EdmMetadata (Id INT PRIMARY KEY,ModelHash TEXT NOT NULL,ProductVersion TEXT NOT NULL ,Model TEXT NOT NULL); ";
            SQLiteCommand cmdCreateTable = new SQLiteCommand(sql, conn);
            cmdCreateTable.ExecuteNonQuery();//如果表不存在，创建数据表   
        }

        private void btnTestDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //SQLiteConnection conn = null;

                //string dbPath = "Data Source =" + Environment.CurrentDirectory + "/UnitTest.db";
                //conn = new SQLiteConnection(dbPath);//创建数据库实例，指定文件位置    
                //conn.Open();//打开数据库，若文件不存在会自动创建    

                //string sql = "select * from BetScoreRecords";
                //SQLiteCommand cmdQ = new SQLiteCommand(sql, conn);

                //SQLiteDataReader reader = cmdQ.ExecuteReader();

                //string result = "";
                //while (reader.Read())
                //{
                //     result += (reader.GetString(0) + " " + reader.GetString(1));
                //}
                //MessageBox.Show(result);
                //conn.Close();

                var time = DateTime.Now;
                using (var db = new SQLiteDB())
                {
                    var hehe = db.BetScoreRecords;
                    if (hehe != null)
                    {
                        var haha = hehe.ToString();
                        MessageBox.Show( (DateTime.Now - time).TotalMilliseconds.ToString());
                    }
                }
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException ex)
            {

            }
        }
    }
}
