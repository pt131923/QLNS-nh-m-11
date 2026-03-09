// Cấu hình kết nối API và SignalR. Chỉ cần apiUrl và hubUrl.
const apiHost = 'http://localhost:5002';

export const environment = {
  production: false,
  apiUrl: `${apiHost}/api`,
  hubUrl: `${apiHost}/dashboard-hub`
};
