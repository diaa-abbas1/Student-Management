using System;
using System.Data;
// 1. استخدام مكتبة SQL Server بدلاً من MySQL
using System.Data.SqlClient;
using System.Windows.Forms;

// NOTE:
// This helper class manages all database operations including connection handling,
// parameterized queries, and CRUD operations.
// Added by <محمد فائد علي عثمان > to improve documentation clarity and maintainability.

public static class DatabaseHelper
{
    public static class DatabaseHelper
{
    // 2. سلسلة الاتصال لـ SQL Server Express
    // Data Source: اسم السيرفر الذي قمنا بتثبيته
    // Initial Catalog: اسم قاعدة البيانات
    // Integrated Security=True: الاتصال عبر Windows Authentication (الأسهل والأكثر أمانًا محليًا)
    private static string connectionString = "Data Source=localhost\\SQLEXPRESS01;Initial Catalog=school_db;Integrated Security=True;TrustServerCertificate=True";

    // دالة مساعدة لفتح اتصال (تستخدم SqlConnection الآن)
    private static SqlConnection GetConnection()
    {
        var conn = new SqlConnection(connectionString);
        conn.Open();
        return conn;
    }

    // دالة جديدة لاختبار الاتصال عند بدء التشغيل
    public static bool TestConnection()
    {
        try
        {
            using (var conn = GetConnection())
            {
                // إذا نجح الاتصال ووصلنا إلى هنا، فكل شيء تمام
                return true;
            }
        }
        catch (SqlException ex) // تم تغيير نوع الاستثناء
        {
            MessageBox.Show($"فشل الاتصال بقاعدة بيانات SQL Server:\n{ex.Message}", "خطأ في الاتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // ---------------------------------------------
    // --- عمليات الطلاب (Students) ---
    // ---------------------------------------------

    public static DataTable GetStudents()
    {
        var dt = new DataTable();
        using (var conn = GetConnection())
        {
            // تم تغيير CONCAT في MySQL إلى + في SQL Server
            string query = "SELECT *, first_name + ' ' + last_name AS FullName FROM Students;";
            // تم استبدال MySqlDataAdapter بـ SqlDataAdapter
            using (var adapter = new SqlDataAdapter(query, conn))
            {
                adapter.Fill(dt);
            }
        }
        return dt;
    }

    public static void AddStudent(string firstName, string lastName, string className, string address, string phone)
    {
        using (var conn = GetConnection())
        {
            string query = "INSERT INTO Students (first_name, last_name, class_name, address, phone) VALUES (@fName, @lName, @class, @address, @phone);";
            // تم استبدال MySqlCommand بـ SqlCommand
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@fName", firstName);
                cmd.Parameters.AddWithValue("@lName", lastName);
                cmd.Parameters.AddWithValue("@class", className);
                cmd.Parameters.AddWithValue("@address", address);
                cmd.Parameters.AddWithValue("@phone", phone);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void UpdateStudent(int studentId, string firstName, string lastName, string className, string address, string phone)
    {
        using (var conn = GetConnection())
        {
            string query = @"
                UPDATE Students SET 
                    first_name = @fName, 
                    last_name = @lName, 
                    class_name = @class, 
                    address = @address, 
                    phone = @phone
                WHERE student_id = @id;";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", studentId);
                cmd.Parameters.AddWithValue("@fName", firstName);
                cmd.Parameters.AddWithValue("@lName", lastName);
                cmd.Parameters.AddWithValue("@class", className);
                cmd.Parameters.AddWithValue("@address", address);
                cmd.Parameters.AddWithValue("@phone", phone);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void DeleteStudent(int studentId)
    {
        using (var conn = GetConnection())
        {
            string query = "DELETE FROM Students WHERE student_id = @id;";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", studentId);
                cmd.Parameters.AddWithValue("@fName", studentId); // تم تصحيح الخطأ: يجب أن تكون القيمة هي studentId
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ---------------------------------------------
    // --- عمليات المواد (Courses) ---
    // ---------------------------------------------

    public static DataTable GetCourses()
    {
        var dt = new DataTable();
        using (var conn = GetConnection())
        {
            string query = "SELECT * FROM Courses;";
            using (var adapter = new SqlDataAdapter(query, conn))
            {
                adapter.Fill(dt);
            }
        }
        return dt;
    }

    public static void AddCourse(string courseName, string professorName)
    {
        using (var conn = GetConnection())
        {
            string query = "INSERT INTO Courses (course_name, professor_name) VALUES (@cName, @pName);";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@cName", courseName);
                cmd.Parameters.AddWithValue("@pName", professorName);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void UpdateCourse(int courseId, string courseName, string professorName)
    {
        using (var conn = GetConnection())
        {
            string query = "UPDATE Courses SET course_name = @cName, professor_name = @pName WHERE course_id = @id;";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", courseId);
                cmd.Parameters.AddWithValue("@cName", courseName);
                cmd.Parameters.AddWithValue("@pName", professorName);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void DeleteCourse(int courseId)
    {
        using (var conn = GetConnection())
        {
            string query = "DELETE FROM Courses WHERE course_id = @id;";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", courseId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ---------------------------------------------
    // --- عمليات الدرجات (Grades) ---
    // ---------------------------------------------

    public static DataTable GetGradesDetailed()
    {
        var dt = new DataTable();
        using (var conn = GetConnection())
        {
            // تم تغيير CONCAT في MySQL إلى + في SQL Server
            string query = @"
                SELECT 
                    g.grade_id,
                    s.first_name + ' ' + s.last_name AS StudentName, 
                    c.course_name AS CourseName,
                    g.score
                FROM Grades g
                JOIN Students s ON g.student_id = s.student_id
                JOIN Courses c ON g.course_id = c.course_id;";
            using (var adapter = new SqlDataAdapter(query, conn))
            {
                adapter.Fill(dt);
            }
        }
        return dt;
    }

    public static void AddGrade(int studentId, int courseId, int score)
    {
        using (var conn = GetConnection())
        {
            string query = "INSERT INTO Grades (student_id, course_id, score) VALUES (@sId, @cId, @score);";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@sId", studentId);
                cmd.Parameters.AddWithValue("@cId", courseId);
                cmd.Parameters.AddWithValue("@score", score);
                cmd.ExecuteNonQuery();
            }
        }
    }
}