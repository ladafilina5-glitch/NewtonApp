using System;
using System.Collections.Generic;
using Npgsql;
using NewtonApp.Models;

namespace NewtonApp.Database
{
    public class DatabaseHelper
    {
        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=integraldb";

        public DatabaseHelper()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS integrals (
                        id SERIAL PRIMARY KEY,
                        calc_time TIMESTAMP NOT NULL,
                        a DOUBLE PRECISION NOT NULL,
                        b DOUBLE PRECISION NOT NULL,
                        n INTEGER NOT NULL,
                        result DOUBLE PRECISION NOT NULL,
                        elapsed_ms DOUBLE PRECISION NOT NULL,
                        task_count INTEGER NOT NULL
                    )";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка создания таблицы:\n" + ex.Message);
            }
        }

        public bool SaveRecord(IntegralRecord record)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql = @"INSERT INTO integrals 
                    (calc_time, a, b, n, result, elapsed_ms, task_count)
                    VALUES (@t, @a, @b, @n, @r, @e, @tc)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", record.CalcTime);
                cmd.Parameters.AddWithValue("@a", record.A);
                cmd.Parameters.AddWithValue("@b", record.B);
                cmd.Parameters.AddWithValue("@n", record.N);
                cmd.Parameters.AddWithValue("@r", record.Result);
                cmd.Parameters.AddWithValue("@e", record.ElapsedMs);
                cmd.Parameters.AddWithValue("@tc", record.TaskCount);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка сохранения:\n" + ex.Message);
                return false;
            }
        }

        public List<IntegralRecord> GetAll()
        {
            var list = new List<IntegralRecord>();
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT id, calc_time, a, b, n, result, elapsed_ms, task_count FROM integrals ORDER BY calc_time DESC", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new IntegralRecord
                    {
                        Id = reader.GetInt32(0),
                        CalcTime = reader.GetDateTime(1),
                        A = reader.GetDouble(2),
                        B = reader.GetDouble(3),
                        N = reader.GetInt32(4),
                        Result = reader.GetDouble(5),
                        ElapsedMs = reader.GetDouble(6),
                        TaskCount = reader.GetInt32(7)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка загрузки:\n" + ex.Message);
            }
            return list;
        }

        public bool ClearAll()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM integrals", conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch { return false; }
        }

        public bool TestConnection()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}