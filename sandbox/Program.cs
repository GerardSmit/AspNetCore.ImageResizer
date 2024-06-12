using GerardSmit.AspNetCore.ImageResizer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImageResizer(b =>
{
    // b.UseSkia();
    // b.UseImageSharp2();
    b.UseImageSharp3();
});

var app = builder.Build();

app.UseImageResizer();
app.UseStaticFiles();

app.Run();
