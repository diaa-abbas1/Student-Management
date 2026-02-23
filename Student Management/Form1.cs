using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public partial class Form1 : Form
{
    // متغير لحفظ رقم الطالب المحدد حالياً
    private int selectedStudentId = 0;

    public Form1()
    {
        InitializeComponent();

        // ربط أحداث التغيير لتحديث حالة الأزرار فوراً
        txtFirstName.TextChanged += InputFields_TextChanged;
        txtLastName.TextChanged += InputFields_TextChanged;
        txtClassName.TextChanged += InputFields_TextChanged;
        txtAddress.TextChanged += InputFields_TextChanged;
        txtPhone.TextChanged += InputFields_TextChanged;
    }

    // عند تحميل الفورم
    private void Form1_Load(object sender, EventArgs e)
    {
        try
        {
            if (DatabaseHelper.TestConnection())
            {
                // إذا نجح الاتصال، نقوم بتحميل بيانات الطلاب
                LoadStudentsGrid();
            }
            else
            {
                MessageBox.Show("فشل الاتصال بقاعدة بيانات SQL Server. تأكد من تشغيل السيرفر وصحة سلسلة الاتصال.", "خطأ فادح", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ غير متوقع: " + ex.Message, "خطأ فادح", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }

        UpdateActionButtons();
    }

    // دالة لتحديث عرض بيانات الطلاب
    private void LoadStudentsGrid()
    {
        try
        {
            DataTable dt = DatabaseHelper.GetStudents();
            dgvStudents.DataSource = dt;

            // تعريب وتهيئة العرض لليمين باستخدام الـ helper
            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "first_name", "الاسم الأول" },
                { "last_name", "الاسم الأخير" },
                { "class_name", "الصف" },
                { "address", "العنوان" },
                { "phone", "رقم الهاتف" },
                { "FullName", "الاسم الكامل" }
            };

            UiHelpers.ConfigureDataGrid(dgvStudents, translations, "student_id");

            // إزالة اختيار الصفوف بعد التحميل
            dgvStudents.ClearSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل بيانات الطلاب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateActionButtons();
        }
    }

    // دالة لمسح الحقول
    private void ClearFields()
    {
        txtFirstName.Text = "";
        txtLastName.Text = "";
        txtClassName.Text = "";
        txtAddress.Text = "";
        txtPhone.Text = "";
        selectedStudentId = 0; // إعادة تعيين الطالب المحدد
        dgvStudents.ClearSelection();
        UpdateActionButtons();
    }

    // عند الضغط على زر "إضافة"
    private void btnAdd_Click(object sender, EventArgs e)
    {
        string validationError;
        if (!ValidateInputs(out validationError))
        {
            MessageBox.Show(validationError, "بيانات ناقصة/غير صحيحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // منع إضافة طالب مكرر (نقارن الاسم الأول والاسم الأخير)
        int existingId;
        if (StudentExists(txtFirstName.Text.Trim(), txtLastName.Text.Trim(), out existingId))
        {
            MessageBox.Show("طالب بنفس الاسم موجود مسبقاً ولا يمكن إضافته مرتين.", "طالب مكرر", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (existingId != 0)
                SelectStudentRowById(existingId);
            return;
        }

        try
        {
            DatabaseHelper.AddStudent(txtFirstName.Text.Trim(), txtLastName.Text.Trim(), txtClassName.Text.Trim(), txtAddress.Text.Trim(), txtPhone.Text.Trim());
            MessageBox.Show("تمت إضافة الطالب بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadStudentsGrid();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ أثناء إضافة الطالب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // عند الضغط على زر "تحديث"
    private void btnUpdate_Click(object sender, EventArgs e)
    {
        if (selectedStudentId == 0)
        {
            MessageBox.Show("الرجاء تحديد طالب من القائمة أولاً لتعديله.", "لم يتم التحديد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string validationError;
        if (!ValidateInputs(out validationError))
        {
            MessageBox.Show(validationError, "بيانات ناقصة/غير صحيحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // منع تحديث الطالب ليصبح نفس اسم طالب آخر
        int existingId;
        if (StudentExists(txtFirstName.Text.Trim(), txtLastName.Text.Trim(), out existingId) && existingId != 0 && existingId != selectedStudentId)
        {
            MessageBox.Show("يوجد طالب آخر بنفس الاسم. اختر اسماً مختلفاً أو حدّد الطالب المراد تحديثه.", "تضارب طالب", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (existingId != 0)
                SelectStudentRowById(existingId);
            return;
        }

        try
        {
            DatabaseHelper.UpdateStudent(selectedStudentId, txtFirstName.Text.Trim(), txtLastName.Text.Trim(), txtClassName.Text.Trim(), txtAddress.Text.Trim(), txtPhone.Text.Trim());
            MessageBox.Show("تم تحديث بيانات الطالب بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadStudentsGrid();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ أثناء تحديث الطالب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // عند الضغط على زر "حذف"
    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (selectedStudentId == 0)
        {
            MessageBox.Show("الرجاء تحديد طالب من القائمة أولاً لحذفه.", "لم يتم التحديد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirmResult = MessageBox.Show("هل أنت متأكد من حذف هذا الطالب؟ سيتم حذف جميع درجاته المرتبطة به.", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.DeleteStudent(selectedStudentId);
                MessageBox.Show("تم حذف الطالب ودرجاته بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadStudentsGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حذف الطالب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // عند الضغط على زر "مسح الحقول"
    private void btnClear_Click(object sender, EventArgs e)
    {
        ClearFields();
    }

    // عند النقر على خلية في الجدول (لعرض بياناتها في الحقول)
    private void dgvStudents_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            try
            {
                DataGridViewRow row = dgvStudents.Rows[e.RowIndex];

                // تخزين رقم الطالب المحدد بأمان
                selectedStudentId = 0;
                var idCell = row.Cells["student_id"];
                if (idCell != null && idCell.Value != null && !Convert.IsDBNull(idCell.Value))
                {
                    int parsedId;
                    if (int.TryParse(idCell.Value.ToString(), out parsedId))
                        selectedStudentId = parsedId;
                }

                // ملء الحقول بأمان (تفادي DBNull)
                txtFirstName.Text = GetCellString(row, "first_name");
                txtLastName.Text = GetCellString(row, "last_name");
                txtClassName.Text = GetCellString(row, "class_name");
                txtAddress.Text = GetCellString(row, "address");
                txtPhone.Text = GetCellString(row, "phone");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحديد الطالب: " + ex.Message);
                ClearFields();
            }
            finally
            {
                UpdateActionButtons();
            }
        }
    }

    // --- أزرار التنقل ---
    private void btnManageCourses_Click(object sender, EventArgs e)
    {
        FormCourses formCourses = new FormCourses();
        formCourses.Show();
    }

    private void btnManageGrades_Click(object sender, EventArgs e)
    {
        FormGrades formGrades = new FormGrades();
        formGrades.Show();
    }

    // --- وظائف مساعدة للتحقق وتجربة المستخدم ---
    private bool ValidateInputs(out string errorMessage)
    {
        errorMessage = null;

        // ضروري: الأسماء
        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
        {
            errorMessage = "الاسم الأول والاسم الأخير حقول إجبارية.";
            return false;
        }

        // حدود طول معقولة
        if (txtFirstName.Text.Trim().Length > 100 || txtLastName.Text.Trim().Length > 100)
        {
            errorMessage = "الاسم الأول أو الأخير طويل جداً (أقصى 100 حرف).";
            return false;
        }

        if (txtClassName.Text.Trim().Length > 50)
        {
            errorMessage = "اسم الصف طويل جداً (أقصى 50 حرف).";
            return false;
        }

        if (txtAddress.Text.Trim().Length > 250)
        {
            errorMessage = "العنوان طويل جداً (أقصى 250 حرف).";
            return false;
        }

        // تحقق بسيط لرقم الهاتف (اختياري: إذا تم إدخاله يجب أن يكون أرقام أو مع رموز + - مسموح بها)
        var phone = txtPhone.Text.Trim();
        if (!string.IsNullOrEmpty(phone))
        {
            var phonePattern = @"^[0-9+\-\s]{6,20}$";
            if (!Regex.IsMatch(phone, phonePattern))
            {
                errorMessage = "رقم الهاتف غير صالح. استخدم أرقاماً، و/أو الرموز + و - والمسافات (6-20 خانة).";
                return false;
            }
        }

        return true;
    }

    private string GetCellString(DataGridViewRow row, string columnName)
    {
        try
        {
            var cell = row.Cells[columnName];
            if (cell == null || cell.Value == null || Convert.IsDBNull(cell.Value))
                return string.Empty;
            return cell.Value.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private void UpdateActionButtons()
    {
        // تمكين/تعطيل أزرار التعديل والحذف اعتماداً على وجود تحديد
        bool hasSelection = selectedStudentId != 0;
        btnUpdate.Enabled = hasSelection;
        btnDelete.Enabled = hasSelection;

        // زر الإضافة متاح فقط إذا الحقول الإلزامية ممتلئة
        btnAdd.Enabled = !string.IsNullOrWhiteSpace(txtFirstName.Text) && !string.IsNullOrWhiteSpace(txtLastName.Text);
    }

    private void InputFields_TextChanged(object sender, EventArgs e)
    {
        // تحديث حالة الأزرار عند تغيير أي حقل
        UpdateActionButtons();
    }

    // يفحص إن كان طالب بنفس الاسم موجود مسبقاً (يستخدم قاعدة البيانات لضمان التحقق الأحدث)
    private bool StudentExists(string firstName, string lastName, out int existingStudentId)
    {
        existingStudentId = 0;
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return false;

        try
        {
            var dt = DatabaseHelper.GetStudents();
            if (dt == null)
                return false;

            foreach (DataRow row in dt.Rows)
            {
                if (!dt.Columns.Contains("first_name") || !dt.Columns.Contains("last_name"))
                    continue;

                var fObj = row["first_name"];
                var lObj = row["last_name"];
                if (fObj == null || lObj == null || Convert.IsDBNull(fObj) || Convert.IsDBNull(lObj))
                    continue;

                var f = fObj.ToString().Trim();
                var l = lObj.ToString().Trim();

                if (string.Equals(f, firstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(l, lastName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (dt.Columns.Contains("student_id") && row["student_id"] != null && !Convert.IsDBNull(row["student_id"]))
                    {
                        int id;
                        if (int.TryParse(row["student_id"].ToString(), out id))
                            existingStudentId = id;
                    }
                    return true;
                }
            }
        }
        catch
        {
            // تجاهل الأخطاء الصغيرة أثناء التحقق
        }

        return false;
    }

    // يختار صف الطالب في الـ DataGridView بناءً على المعرف (يساعد المستخدم لرؤية السجل الموجود)
    private void SelectStudentRowById(int studentId)
    {
        try
        {
            if (studentId == 0 || dgvStudents.Rows.Count == 0)
                return;

            foreach (DataGridViewRow row in dgvStudents.Rows)
            {
                var idCell = row.Cells["student_id"];
                if (idCell == null || idCell.Value == null || Convert.IsDBNull(idCell.Value))
                    continue;

                int id;
                if (int.TryParse(idCell.Value.ToString(), out id) && id == studentId)
                {
                    dgvStudents.ClearSelection();
                    row.Selected = true;
                    dgvStudents.CurrentCell = row.Cells.Count > 0 ? row.Cells[0] : null;
                    dgvStudents.FirstDisplayedScrollingRowIndex = row.Index;

                    // ملء الحقول من هذا الصف
                    txtFirstName.Text = GetCellString(row, "first_name");
                    txtLastName.Text = GetCellString(row, "last_name");
                    txtClassName.Text = GetCellString(row, "class_name");
                    txtAddress.Text = GetCellString(row, "address");
                    txtPhone.Text = GetCellString(row, "phone");

                    selectedStudentId = studentId;
                    UpdateActionButtons();
                    return;
                }
            }
        }
        catch
        {
            // تجاهل أي خطأ صغير في اختيار الصف
        }
    }
}