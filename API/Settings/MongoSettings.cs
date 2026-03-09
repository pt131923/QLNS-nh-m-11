using System;

namespace API.Settings
{
    /// <summary>
    /// Cấu hình kết nối MongoDB, đọc từ appsettings.json → section "MongoSettings".
    /// </summary>
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}

