using System;
using API.Entities;
using API.Data;
using Microsoft.AspNetCore.Http;

namespace API.DTOs
{
	public class FileDto
	{
		public IFormFile File {get; set; }
	}
}