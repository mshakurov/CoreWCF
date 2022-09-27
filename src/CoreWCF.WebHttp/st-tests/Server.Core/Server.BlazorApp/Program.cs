using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// 1. ��������� �������������
// ----------------------------
// - ������������ �������
Server.Runner.ServerRunner.Configure(builder.WebHost);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ----------------------------
// 2. ������������� ��������
// ----------------------------
// - ������������� wcf,
// - �������� singleton �������� (�������)
// ----------------------------
Server.Runner.ServerRunner.CreateServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ----------------------------
// 3. ������������ �������������
// ----------------------------
// - �������� wcf �������� � �������� �����
// ----------------------------
Server.Runner.ServerRunner.PrepareServices(app);

app.Run();
