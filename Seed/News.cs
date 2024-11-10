using Microsoft.EntityFrameworkCore;

namespace Gaos.Seed
{
    public class News
    {
        public static void Seed(ModelBuilder modelBuilder, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                // seed Dbo.Api1.Leaderboard
                modelBuilder.Entity<Gaos.Dbo.Model.NewsData>().HasData(
                    new Gaos.Dbo.Model.NewsData 
                    { 
                        ImageName = "Update001Image.png", 
                        Id = 1, 
                        Title= "Patch Update 0.0.1",
                        Headline = "Exploration and combat features added with multiple new monster and vast amount of dungeons!",
                        Details =
                            "<p>Hello explorers,</p>" +
                            "<p>we are prooud to present you another patch from our works that addresses combat, multiplayer and so many other features.<br>" +
                            "The combat is time turn based meaning that every combatant has to have their 'timebar' filled up so they can get their turn.</p><br>", 
                        Link= "update-001" 
                    },
                    new Gaos.Dbo.Model.NewsData 
                    { 
                        ImageName = "Update002Image.png", 
                        Id = 2, 
                        Title = "Patch Update 0.0.2",
                        Headline = "New harvestable materials, craftable recipees and many building types added for your base!",
                        Details =
                            "<p>Dear space crafters,</p>" +
                            "<p>The crafting update is here that brings a ton of new recipes and manufacturing options that further expands GAO craftoverse!<br>" +
                            "New recipes, materials, and buildings were added to smooth out the early game and include more options for the player:</p>" +
                            "<ul>" +
                                "<li>Iron</li>" +
                                "<li>Iron pipe</li>" +
                                "<li>Coal</li>" +
                                "<li>Steel</li>" +
                                "<li>Steel beam</li>" +
                                "<li>Research device</li>" +
                                "<li>Fibrous leaves farm</li>" +
                                "<li>Steam generator</li>" +
                            "</ul>",
                        Link = "update-002" }
                );
            }
        }
    }
}

