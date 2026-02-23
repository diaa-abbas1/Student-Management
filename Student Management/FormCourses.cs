using System;
using System.Data;
using System.Windows.Forms;


public partial class FormCourses : Form
{
    private int selectedCourseId = 0;

    public FormCourses()
    {
        InitializeComponent();

        // ربط أحداث التغيير لتحديث حالة الأزرار فوراً
        txtCourseName.TextChanged += InputFields_TextChanged;
        txtProfessorName.TextChanged += InputFields_TextChanged;

        // تأكد أن حالة الأزرار متوافقة مع الوضع الابتدائي
        UpdateActionButtons();
    }

    private void FormCourses_Load(object sender, EventArgs e)
    {
        LoadCoursesGrid();
        UpdateActionButtons();
    }

    private void LoadCoursesGrid()
    {
        try
        {
            dgvCourses.DataSource = DatabaseHelper.GetCourses();

            // تعريب العناوين (تغيير ما يظهر للمستخدم فقط)
            if (dgvCourses.Columns["course_name"] != null)
                dgvCourses.Columns["course_name"].HeaderText = "اسم المادة";

            if (dgvCourses.Columns["professor_name"] != null)
                dgvCourses.Columns["professor_name"].HeaderText = "اسم الأستاذ";

            // إخفاء عمود الـ ID (اختياري، لكنه مفيد للمبرمج)
            if (dgvCourses.Columns["course_id"] != null)
                dgvCourses.Columns["course_id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل المواد: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // بعد كل تحميل، حدّث حالة الأزرار (خاصّة زر الحذف والتحديث والإضافة)
            UpdateActionButtons();
        }
    }

    private void ClearFields()
    {
        txtCourseName.Text = "";
        txtProfessorName.Text = "";
        selectedCourseId = 0;
        dgvCourses.ClearSelection();
        UpdateActionButtons();
    }

    private void btnAddCourse_Click(object sender, EventArgs e)
    {
        string validationError;
        if (!ValidateInputs(out validationError))
        {
            MessageBox.Show(validationError, "بيانات ناقصة/غير صحيحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // منع إضافة مادة مكررة
        int existingId;
        if (CourseExists(txtCourseName.Text.Trim(), out existingId))
        {
            // إذا المادة موجودة، نخبر المستخدم ونحددها في الجدول
            MessageBox.Show("المادة موجودة مسبقاً ولا يمكن إضافتها مرتين.", "مادة مكررة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (existingId != 0)
                SelectCourseRowById(existingId);
            return;
        }

        try
        {
            DatabaseHelper.AddCourse(txtCourseName.Text.Trim(), txtProfessorName.Text.Trim());
            MessageBox.Show("تمت إضافة المادة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadCoursesGrid();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ أثناء إضافة المادة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnUpdateCourse_Click(object sender, EventArgs e)
    {
        if (selectedCourseId == 0)
        {
            MessageBox.Show("الرجاء تحديد مادة لتعديلها.", "لم يتم التحديد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string validationError;
        if (!ValidateInputs(out validationError))
        {
            MessageBox.Show(validationError, "بيانات ناقصة/غير صحيحة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // منع تحديث الاسم إلى اسم مادة موجودة بالفعل (تحقق من الوجود ومعرف آخر)
        int existingId;
        if (CourseExists(txtCourseName.Text.Trim(), out existingId) && existingId != 0 && existingId != selectedCourseId)
        {
            MessageBox.Show("اسم المادة هذا مستخدم من قبل مادة أخرى. اختر اسمًا مختلفًا.", "تضارب اسم المادة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (existingId != 0)
                SelectCourseRowById(existingId);
            return;
        }

        try
        {
            DatabaseHelper.UpdateCourse(selectedCourseId, txtCourseName.Text.Trim(), txtProfessorName.Text.Trim());
            MessageBox.Show("تم تحديث المادة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadCoursesGrid();
            ClearFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ أثناء تحديث المادة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnDeleteCourse_Click(object sender, EventArgs e)
    {
        // إذا لم يتم اختيار مادة، لا تفعل شيئاً — الأزرار عادة ما تكون معطلة
        if (selectedCourseId == 0)
            return;

        var confirmResult = MessageBox.Show("هل أنت متأكد من حذف هذه المادة؟ سيتم حذف جميع الدرجات المرتبطة بها.", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.DeleteCourse(selectedCourseId);
                MessageBox.Show("تم حذف المادة ودرجاتها بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCoursesGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حذف المادة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void dgvCourses_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            try
            {
                DataGridViewRow row = dgvCourses.Rows[e.RowIndex];

                // قراءة المعرف بأمان
                selectedCourseId = 0;
                var idCell = row.Cells["course_id"];
                if (idCell != null && idCell.Value != null && !Convert.IsDBNull(idCell.Value))
                {
                    int parsedId;
                    if (int.TryParse(idCell.Value.ToString(), out parsedId))
                        selectedCourseId = parsedId;
                }

                // ملء الحقول بأمان
                txtCourseName.Text = GetCellString(row, "course_name");
                txtProfessorName.Text = GetCellString(row, "professor_name");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحديد المادة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearFields();
            }
            finally
            {
                // بعد اختيار صف، فعّل/عطّل الأزرار المناسبة
                UpdateActionButtons();
            }
        }
    }

    // --- وظائف مساعدة للتحقق وتجربة المستخدم ---
    private bool ValidateInputs(out string errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(txtCourseName.Text))
        {
            errorMessage = "اسم المادة حقل إجباري.";
            return false;
        }

        if (txtCourseName.Text.Trim().Length > 150)
        {
            errorMessage = "اسم المادة طويل جداً (أقصى 150 حرف).";
            return false;
        }

        if (txtProfessorName.Text.Trim().Length > 100)
        {
            errorMessage = "اسم الأستاذ طويل جداً (أقصى 100 حرف).";
            return false;
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
        bool hasSelection = (selectedCourseId != 0);

        // تمكين/تعطيل أزرار التعديل والحذف اعتماداً على وجود تحديد
        btnUpdateCourse.Enabled = hasSelection;
        btnDeleteCourse.Enabled = hasSelection;

        // زر الإضافة متاح فقط إذا الحقول الإلزامية ممتلئة
        btnAddCourse.Enabled = !string.IsNullOrWhiteSpace(txtCourseName.Text);
    }

    private void InputFields_TextChanged(object sender, EventArgs e)
    {
        // تحديث حالة الأزرار عند تغيير أي حقل
        UpdateActionButtons();
    }

    // يفحص إن كانت مادة بنفس الاسم موجودة مسبقاً (يستخدم قاعدة البيانات لضمان التحقق الأحدث)
    private bool CourseExists(string courseName, out int existingCourseId)
    {
        existingCourseId = 0;
        if (string.IsNullOrWhiteSpace(courseName))
            return false;

        try
        {
            var dt = DatabaseHelper.GetCourses();
            if (dt == null)
                return false;

            foreach (DataRow row in dt.Rows)
            {
                if (!dt.Columns.Contains("course_name"))
                    continue;

                var nameObj = row["course_name"];
                if (nameObj == null || Convert.IsDBNull(nameObj))
                    continue;

                var name = nameObj.ToString().Trim();
                if (string.Equals(name, courseName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (dt.Columns.Contains("course_id") && row["course_id"] != null && !Convert.IsDBNull(row["course_id"]))
                    {
                        int id;
                        if (int.TryParse(row["course_id"].ToString(), out id))
                            existingCourseId = id;
                    }
                    return true;
                }
            }
        }
        catch
        {
            // إذا فشل استدعاء قاعدة البيانات، لا نريد رمي استثناء هنا — نعيد false
        }

        return false;
    }

    // يختار صف المادة في الـ DataGridView بناءً على المعرف (يسمح للمستخدم برؤية المادة الموجودة)
    private void SelectCourseRowById(int courseId)
    {
        try
        {
            if (courseId == 0 || dgvCourses.Rows.Count == 0)
                return;

            foreach (DataGridViewRow row in dgvCourses.Rows)
            {
                var idCell = row.Cells["course_id"];
                if (idCell == null || idCell.Value == null || Convert.IsDBNull(idCell.Value))
                    continue;

                int id;
                if (int.TryParse(idCell.Value.ToString(), out id) && id == courseId)
                {
                    dgvCourses.ClearSelection();
                    row.Selected = true;
                    dgvCourses.CurrentCell = row.Cells.Count > 0 ? row.Cells[0] : null;
                    // تمرير إلى الصف ليظهر للمستخدم
                    dgvCourses.FirstDisplayedScrollingRowIndex = row.Index;
                    // ملء الحقول من هذا الصف
                    txtCourseName.Text = GetCellString(row, "course_name");
                    txtProfessorName.Text = GetCellString(row, "professor_name");
                    selectedCourseId = courseId;
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