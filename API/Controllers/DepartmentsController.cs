using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;

namespace API.Controllers
{

	[ApiController]
	[Route("api/[controller]")]
	public class DepartmentsController(IDepartmentRepository _departmentRepository, IMapper _mapper) : BaseApiController
	{
		[HttpGet]
		public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
		{
			var departments = await _departmentRepository.GetDepartmentAsync();
			return Ok(departments);
		}

		[HttpGet("{id}", Name = "GetDepartmentById")]
		public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id)
		{
			var department = await _departmentRepository.GetDepartmentByIdAsync(id);

			if (department == null)
				return NotFound();

			return Ok(department);
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> UpdateDepartment(int id, DepartmentUpdateDto departmentUpdateDto)
		{
			var department = await _departmentRepository.GetDepartmentByIdAsync(id);

			if (department == null)
				return NotFound();

			_mapper.Map(departmentUpdateDto, department);
			_departmentRepository.Update(department);

			if (await _departmentRepository.SaveAllAsync())
				return NoContent();

			return BadRequest("Failed to update Department");
		}

		[HttpPost("add-department")]
		public async Task<ActionResult<DepartmentDto>> AddDepartment(DepartmentDto departmentDto)
		{
			if (departmentDto == null || string.IsNullOrWhiteSpace(departmentDto.Name))
				return BadRequest("Department name is required");

			if (await _departmentRepository.DepartmentExists(departmentDto.Name))
				return BadRequest("Department name is already taken");

			var department = _mapper.Map<AppDepartment>(departmentDto);

			_departmentRepository.Add(department);

			await _departmentRepository.SaveAllAsync();

			return CreatedAtRoute(
				"GetDepartmentById",
				new { id = department.DepartmentId },
				_mapper.Map<DepartmentDto>(department)
			);
		}

		[HttpDelete("delete-department/{id}")]
		public async Task<ActionResult> DeleteDepartment(int id)
		{
			var department = await _departmentRepository.GetDepartmentByIdAsync(id);

			if (department == null)
				return NotFound();

			_departmentRepository.Delete(department);

			if (await _departmentRepository.SaveAllAsync())
				return Ok();

			return BadRequest("Failed to delete the Department");
		}

		[HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
           string name = null,
           string description = null,
           int? soNhanVien = null,
           string address = null,
           string note = null)
    {
    // ✅ 1. Xây dựng bộ lọc đúng cho AppDepartment
    var filter = Builders<AppDepartment>.Filter.Empty;

    if (!string.IsNullOrWhiteSpace(name))
        filter &= Builders<AppDepartment>.Filter.Regex(
            x => x.Name,
            new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(name.Trim()), "i"));

    if (!string.IsNullOrWhiteSpace(description))
        filter &= Builders<AppDepartment>.Filter.Regex(
            x => x.Description,
            new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(description.Trim()), "i"));

    if (soNhanVien.HasValue)
        filter &= Builders<AppDepartment>.Filter.Eq(x => x.SlNhanVien, soNhanVien.Value);

    if (!string.IsNullOrWhiteSpace(address))
        filter &= Builders<AppDepartment>.Filter.Regex(
            x => x.Addresses,
            new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(address.Trim()), "i"));

    if (!string.IsNullOrWhiteSpace(note))
        filter &= Builders<AppDepartment>.Filter.Regex(
            x => x.Notes,
            new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(note.Trim()), "i"));

    // ✅ 2. Lấy dữ liệu đúng model
    var departmentList = await _departmentRepository.GetFilteredDepartmentsAsync(filter);

    // ✅ 3. Cấu hình Excel đúng với cấu trúc Phòng ban
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    using var package = new ExcelPackage();
    var ws = package.Workbook.Worksheets.Add("Danh sách phòng ban");

    var headers = new[]
    {
        "STT", "Mã PB", "Tên phòng ban", "Mô tả",
        "Số nhân viên", "Địa điểm", "Ghi chú"
    };

    // Định dạng header
    for (int i = 0; i < headers.Length; i++)
    {
        var cell = ws.Cells[1, i + 1];
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
        cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
    }

    // ✅ 4. Ghi dữ liệu đúng các trường
    for (int i = 0; i < departmentList.Count; i++)
    {
        var dept = departmentList[i];
        var row = i + 2;

        ws.Cells[row, 1].Value = i + 1;                    
        ws.Cells[row, 2].Value = dept.DepartmentId;        
        ws.Cells[row, 3].Value = dept.Name;                 
        ws.Cells[row, 4].Value = dept.Description;          
        ws.Cells[row, 5].Value = dept.SlNhanVien;        
        ws.Cells[row, 6].Value = dept.Addresses;             
        ws.Cells[row, 7].Value = dept.Notes;                

        if (i % 2 == 1)
        {
            var range = ws.Cells[row, 1, row, headers.Length];
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(217, 225, 242));
        }
    }

    ws.Cells[ws.Dimension.Address].AutoFitColumns();

    // ✅ 5. Trả về file Excel
    var bytes = await package.GetAsByteArrayAsync();
    return File(
        bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"DanhSachPhongBan_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
  }
}


