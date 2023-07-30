namespace Villainous.Server.Application;

public static class ApplicationConfigurator
{
    public static void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapHub<GameHub>("/gameHub");

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHttpsRedirection();
    }
}