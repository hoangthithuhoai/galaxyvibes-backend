using Galaxyvibes.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CẤU HÌNH CORS (MỞ CỬA CHO REACT WEB & EXPO APP)
// ==========================================
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowReactApp",
		policy =>
		{
			// Thay vì chỉ fix cứng localhost:5173, ta mở luôn cho tiện test App điện thoại
			policy.AllowAnyOrigin()
				  .AllowAnyHeader()
				  .AllowAnyMethod();
		});
});

// ==========================================
// 2. CẤU HÌNH DATABASE
// ==========================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ==========================================
// 3. CẤU HÌNH CONTROLLERS & SWAGGER
// ==========================================
builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // ← Thêm dòng này
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// 4. CẤU HÌNH BẢO MẬT JWT (CẤP THẺ BÀI)
// ==========================================
// Các thông số này được lấy khớp chính xác với AuthController của bạn
var secretKey = "Chuoi_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan_123!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = "ThoughtGalaxyServer",
			ValidAudience = "ThoughtGalaxyClient",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
		};
	});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
}
app.UseStaticFiles();
// KÍCH HOẠT CORS - Lệnh này bắt buộc phải nằm TRƯỚC xác thực bảo mật
app.UseCors("AllowReactApp");

// ==========================================
// 5. CẤU HÌNH ĐƯỜNG ỐNG XỬ LÝ (PIPELINE)
// ==========================================
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

//app.UseHttpsRedirection();


// KÍCH HOẠT XÁC THỰC VÀ BẢO MẬT
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles(); // Cho phép truy cập các file trong thư mục wwwroot

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.EnsureCreated(); // đảm bảo bảng được tạo nếu chưa có
}

app.Run();