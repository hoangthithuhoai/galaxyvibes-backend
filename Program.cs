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
			policy.AllowAnyOrigin()
				  .AllowAnyHeader()
				  .AllowAnyMethod();
		});
});

// ==========================================
// 2. CẤU HÌNH DATABASE (Supabase PostgreSQL)
// ==========================================
var connectionString = "Host=aws-0-ap-southeast-1.pooler.supabase.co;Port=6543;Database=postgres;Username=postgres.fyzobyyrirowaklfzilg;Password=Matkhaucuahoai;SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString)
);

// ==========================================
// 3. CẤU HÌNH CONTROLLERS & SWAGGER
// ==========================================
builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// 4. CẤU HÌNH BẢO MẬT JWT (CẤP THẺ BÀI)
// ==========================================
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

// ==========================================
// 5. TỰ ĐỘNG MIGRATE DATABASE KHI KHỞI ĐỘNG
// ==========================================
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate(); // Tự động tạo bảng nếu chưa có
}

app.UseStaticFiles();
app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();