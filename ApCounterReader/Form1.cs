using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data.SQLite;


namespace ApCounterReader
{
    public partial class Form1 : Form
    {
        // Parametry po³¹czenia do bazy MySQL na serwerze Webio
        string mysqlConnStr = "server=adres_serwera_webio;user=twoj_uzytkownik;password=twoje_haslo;database=twoja_baza;";

        // Œcie¿ka do lokalnej bazy SQLite
        string sqliteDbFile = "localdb.sqlite";
        string sqliteConnStr;


        public Form1()
        {
            InitializeComponent();
            sqliteConnStr = $"Data Source={sqliteDbFile};Version=3;";
        }

        private void Form1_Load(object sender, EventArgs e)
        {



        }

        private void btnTransfer_Click(object sender, EventArgs e)
        {
            // Wykonaj transfer danych z MySQL do SQLite
            TransferData();
            // Za³aduj dane z lokalnej bazy SQLite do DataGridView
            LoadDataToGrid();
        }

        private void TransferData()
        {
            // Utworzenie lokalnej bazy SQLite, jeœli nie istnieje
            if (!System.IO.File.Exists(sqliteDbFile))
            {
                SQLiteConnection.CreateFile(sqliteDbFile);
            }

            // Utworzenie tabeli counterRead w bazie SQLite, jeœli nie istnieje
            using (var sqliteConn = new SQLiteConnection(sqliteConnStr))
            {
                sqliteConn.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS counterRead (
                        idCustomer INTEGER,
                        readDate TEXT,
                        readTime TEXT,
                        counterState INTEGER
                    );";
                using (var sqliteCmd = new SQLiteCommand(createTableQuery, sqliteConn))
                {
                    sqliteCmd.ExecuteNonQuery();
                }
                sqliteConn.Close();
            }

            // Pobranie danych z bazy MySQL
            DataTable dt = new DataTable();
            using (var mysqlConn = new MySqlConnection(mysqlConnStr))
            {
                try
                {
                    mysqlConn.Open();
                    string selectQuery = "SELECT idCustomer, readDate, readTime, counterState FROM counterTable";
                    using (var mysqlCmd = new MySqlCommand(selectQuery, mysqlConn))
                    {
                        using (var reader = mysqlCmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("B³¹d podczas pobierania danych z MySQL: " + ex.Message, "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    mysqlConn.Close();
                }

                // Wstawienie pobranych danych do lokalnej bazy SQLite
                using (var sqliteConn = new SQLiteConnection(sqliteConnStr))
                {
                    sqliteConn.Open();
                    // Opcjonalnie: wyczyszczenie poprzednich danych, by nie powielaæ wpisów
                    string deleteQuery = "DELETE FROM counterRead";
                    using (var sqliteCmd = new SQLiteCommand(deleteQuery, sqliteConn))
                    {
                        sqliteCmd.ExecuteNonQuery();
                    }

                    // Wstawianie danych
                    foreach (DataRow row in dt.Rows)
                    {
                        string insertQuery = @"
                        INSERT INTO counterRead (idCustomer, readDate, readTime, counterState)
                        VALUES (@idCustomer, @readDate, @readTime, @counterState)";
                        using (var sqliteCmd = new SQLiteCommand(insertQuery, sqliteConn))
                        {
                            sqliteCmd.Parameters.AddWithValue("@idCustomer", row["idCustomer"]);
                            sqliteCmd.Parameters.AddWithValue("@readDate", row["readDate"]);
                            sqliteCmd.Parameters.AddWithValue("@readTime", row["readTime"]);
                            sqliteCmd.Parameters.AddWithValue("@counterState", row["counterState"]);
                            sqliteCmd.ExecuteNonQuery();
                        }
                    }
                    sqliteConn.Close();
                }
            }

        }

        private void LoadDataToGrid()
        {
            DataTable dt = new DataTable();
            using (var sqliteConn = new SQLiteConnection(sqliteConnStr))
            {
                sqliteConn.Open();
                string selectQuery = "SELECT idCustomer, readDate, readTime, counterState FROM counterRead";
                using (var cmd = new SQLiteCommand(selectQuery, sqliteConn))
                {
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
                sqliteConn.Close();
            }

            dataGridView1.DataSource = dt;
        }


    }
}
