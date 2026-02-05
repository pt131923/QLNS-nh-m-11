using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR; // Đảm bảo đã import thư viện này

namespace API.Hubs // Đã đổi Namespace thành API.Hubs để khớp với DashboardController
{
    [Authorize]
    public class DashboardHub : Hub 
    {
        // 1. Phương thức OnConnectedAsync: Được gọi khi một client kết nối đến Hub
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        // 2. Phương thức OnDisconnectedAsync: Được gọi khi một client ngắt kết nối
        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
        
        // *Ghi chú: Bạn không cần định nghĩa các phương thức gửi dữ liệu tại đây, 
        // vì việc gửi dữ liệu được thực hiện bởi IHubContext trong DashboardController.*
        
        // *Tuy nhiên, bạn có thể định nghĩa các phương thức mà Front-end có thể gọi, ví dụ:*
        /*
        public async Task RequestInitialData()
        {
            // Logic để gửi dữ liệu ban đầu cho client vừa kết nối
            // Ví dụ: gọi dịch vụ để lấy dữ liệu dashboard và gửi lại cho Context.ConnectionId
            var initialData = new { totalEmployees = 100, activeContracts = 50 }; // Dữ liệu giả định
            await Clients.Caller.SendAsync("ReceiveDashboardUpdate", initialData);
        }
        */
    }
}