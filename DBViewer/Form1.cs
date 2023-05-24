using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;

namespace DBViewer
{
    public partial class Form1 : Form
    {
        DataTable datatable;
        DataSet1 dataset;
        public static void CreateTable(string str, SQLiteConnection connection)
        {
            SQLiteCommand CreateCommand = new SQLiteCommand(str, connection);
            CreateCommand.ExecuteNonQuery();
        }
        public static void InsertTable(string str, SQLiteConnection connection)
        {
            SQLiteCommand CreateCommand = new SQLiteCommand(str, connection);
            CreateCommand.ExecuteNonQuery();
        }
        public Form1()
        {
            datatable = new DataTable();
            InitializeComponent();
            dataset = new DataSet1();
            string databasePath = "D:\\FlappyBird2D\\FlappyBird2D.db";

            // 경로에 db 파일 생성하기
            if (!File.Exists(databasePath))
            {
                File.Create(databasePath);
            }

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + databasePath))
            {
                connection.Open();

                // 게임 결과 데이터 받아오기 (Date는 게임이 종료될때의 시간)
                string id = "채성준";
                string password = "1234";
                int score = 0;
                DateTime date = DateTime.Now;
                string playTime = "1 minutes";

                // ID 별로 새로운 테이블 생성하기 (날짜를 기본키로 설정 : 플레이한 시간이 다르기 때문)
                string idTable = string.Format("{0}{1}", id, "_table");
                string createidTableQuery = string.Format("CREATE TABLE IF NOT EXISTS {0} (Id TEXT, Password TEXT, Score INTEGER, Date TEXT PRIMARY KEY, PlayTime TEXT);", idTable);
                CreateTable(createidTableQuery, connection);

                // 생성된 테이블에 데이터 삽입하기
                string InsertidTableQuery = string.Format("INSERT OR IGNORE INTO {0} (Id, Password, Score, Date, PlayTime) VALUES ('{1}', '{2}', {3}, '{4}', '{5}');", idTable, id, password, score, date.ToString("yyyy-MM-dd HH:mm:ss"), playTime);
                InsertTable(InsertidTableQuery, connection);

                // 새로운 rankTable 테이블 생성 SQL 쿼리 작성 (아이디를 기본키로 설정 : 아이디가 유저마다 다르기 때문)
                string rank = "rank_table";
                string rankTableQuery = string.Format("CREATE TABLE IF NOT EXISTS {0} (Id TEXT PRIMARY KEY, Password TEXT, Score INTEGER, Date TEXT, PlayTime TEXT);", rank);
                CreateTable(rankTableQuery, connection);

                // db파일에 있는 테이블들 이름을 for문으로 반환
                DataTable tables = connection.GetSchema("Tables");
                List<string> tablelist = new List<string>();
                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    if (tableName != "rank_table")
                    {
                        tablelist.Add(tableName);
                    }
                }

                foreach (string str in tablelist)
                {
                    string tableName = str;
                    // SELECT 구문을 활용하여 제일 높은 점수를 가진 튜플만 선택 및 dataset에 아이디랑 비밀번호 저장
                    SQLiteCommand command = new SQLiteCommand(connection);
                    string selectBestQuery = string.Format("SELECT * FROM {0} ORDER BY Score desc limit (1)", tableName);
                    command.CommandText = selectBestQuery;

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string bestId = reader.GetString(0);
                            string bestPassword = reader.GetString(1);
                            int bestScore = reader.GetInt32(2);
                            string bestDate = reader.GetString(3);
                            string bestTime = reader.GetString(4);

                            dataset.Tables["users"].Rows.Add(new object[] {bestId, bestPassword});

                            // rank_table에 Id별로 점수가 높은 튜플만 INSERT
                            string updateRankTableQuery = string.Format("INSERT OR IGNORE INTO {0} (Id, Password, Score, Date, PlayTime) VALUES ('{1}', '{2}', {3}, '{4}', '{5}') ON CONFLICT" +
                                "(Id) DO UPDATE SET Score = excluded.Score, Password = excluded.Password, Date = excluded.Date, PlayTime = excluded.PlayTime;", rank, bestId, bestPassword, bestScore, bestDate, bestTime);
                            InsertTable(updateRankTableQuery, connection);
                        }
                    }
                }

                SQLiteCommand rankCommand = new SQLiteCommand(connection);
                string selectRankQuery = string.Format("SELECT * FROM {0} ORDER BY Score desc limit(3)", "rank_table");
                rankCommand.CommandText = selectRankQuery;
                using (SQLiteDataReader reader = rankCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string rankId = reader.GetString(0);
                        int rankScore = reader.GetInt32(2);
                        string rankDate = reader.GetString(3);
                        string rankTime = reader.GetString(4);
                        dataset.Tables["rank"].Rows.Add(new object[] { rankId, rankScore, rankDate, rankTime });
                    }
                }
                // connection 닫기
                connection.Close();
            }

        }

        public void showbtn_Click(object sender, EventArgs e)
        {
            dataset.Tables["play"].Clear();
            if (textBox1.Text == "users" | textBox1.Text == "rank")
            {
                dataGridView1.DataSource = dataset.Tables[textBox1.Text];
            }
            else
            {
                string name = textBox1.Text;
                string tableName = name + "_table";
                string databasePath = "D:\\FlappyBird2D\\FlappyBird2D.db";
                using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + databasePath))
                {
                    connection.Open();
                    SQLiteCommand selectCommand = new SQLiteCommand(connection);
                    string selectTableQuery = string.Format("SELECT * FROM {0} ORDER BY Score desc;", tableName);
                    selectCommand.CommandText = selectTableQuery;
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rid = reader.GetString(0);
                            int rscore = reader.GetInt32(2);
                            string rdate = reader.GetString(3);
                            string rplaytime = reader.GetString(4);
                            dataset.Tables["play"].Rows.Add(new object[] { rid, rscore, rdate, rplaytime });
                        }
                    }
                    connection.Close();
                }
                dataGridView1.DataSource = dataset.Tables["play"];
            }
        }
    }
}
