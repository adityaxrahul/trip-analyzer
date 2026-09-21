using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TripAnalyzer.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Models.ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Pre-migration duplicates cleanup to ensure unique index IX_Districts_StateCode_DistrictCode can be created
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM ""Districts"" 
                    WHERE ""Id"" NOT IN (
                        SELECT MIN(""Id"") 
                        FROM ""Districts"" 
                        GROUP BY COALESCE(""StateCode"", ''), COALESCE(""DistrictCode"", '')
                    );
                ");

                await context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM ""Districts"" 
                    WHERE ""StateCode"" IS NULL OR ""StateCode"" = '' 
                       OR ""DistrictCode"" IS NULL OR ""DistrictCode"" = '';
                ");
            }
            catch (Exception)
            {
                // Table might not exist yet, safe to ignore
            }

            // Migrate database automatically
            await context.Database.MigrateAsync();

            // 1. Roles
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Controlled Admin User Initialization
            string? adminEmail = configuration["ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            string? adminPassword = configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            // Default fallback only if not explicitly configured in production
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                adminEmail = "admin@tripanalyzer.com";
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                if (!string.IsNullOrWhiteSpace(adminPassword))
                {
                    var adminUser = new Models.ApplicationUser
                    {
                        UserName = adminEmail.Trim(),
                        Email = adminEmail.Trim(),
                        FullName = "Trip Analyzer Administrator",
                        MobileNumber = "+91 9876543210",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                {
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
                }
            }

            // 3. Demo Transport Options
            if (!await context.TransportOptions.AnyAsync())
            {
                var sampleOptions = new List<Models.TransportOption>
                {
                    // Rajkot -> Mumbai (~700 km)
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Mumbai",
                        Mode = "Train",
                        CarrierName = "Saurashtra Mail Superfast Express",
                        BasePricePerPassenger = 800m,
                        DurationMinutes = 720, // 12h
                        DistanceKm = 700,
                        AvailabilityScore = 90.0,
                        ComfortScore = 82.0,
                        EcoRating = 4.8,
                        DepartureTime = "02:15 PM",
                        ArrivalTime = "02:15 AM",
                        Amenities = "AC 3-Tier, Sleeping Berth, Onboard Catering, Charging Outlets",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Mumbai",
                        Mode = "Bus",
                        CarrierName = "Eagle Travels Luxury Sleeper AC",
                        BasePricePerPassenger = 1000m,
                        DurationMinutes = 840, // 14h
                        DistanceKm = 700,
                        AvailabilityScore = 75.0,
                        ComfortScore = 70.0,
                        EcoRating = 3.5,
                        DepartureTime = "06:30 PM",
                        ArrivalTime = "08:30 AM",
                        Amenities = "AC Sleeper, USB Port, Reading Light, Live Tracking",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Mumbai",
                        Mode = "Flight",
                        CarrierName = "Air India Express (Direct)",
                        BasePricePerPassenger = 4500m,
                        DurationMinutes = 90, // 1h 30m
                        DistanceKm = 700,
                        AvailabilityScore = 85.0,
                        ComfortScore = 92.0,
                        EcoRating = 2.5,
                        DepartureTime = "11:45 AM",
                        ArrivalTime = "01:15 PM",
                        Amenities = "In-flight Meal, Complimentary Beverage, 15kg Check-in Bag",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Mumbai",
                        Mode = "Car",
                        CarrierName = "Intercity SUV Cab Share",
                        BasePricePerPassenger = 2800m,
                        DurationMinutes = 660, // 11h
                        DistanceKm = 700,
                        AvailabilityScore = 70.0,
                        ComfortScore = 80.0,
                        EcoRating = 3.0,
                        DepartureTime = "Flexible",
                        ArrivalTime = "Flexible",
                        Amenities = "Doorstep Pickup, Private AC Cab, Recliners",
                        IsActive = true,
                        IsEstimatedData = true
                    },

                    // Rajkot -> Ahmedabad (~220 km)
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Ahmedabad",
                        Mode = "Train",
                        CarrierName = "Vande Bharat Express",
                        BasePricePerPassenger = 450m,
                        DurationMinutes = 195, // 3h 15m
                        DistanceKm = 220,
                        AvailabilityScore = 95.0,
                        ComfortScore = 95.0,
                        EcoRating = 4.9,
                        DepartureTime = "06:00 AM",
                        ArrivalTime = "09:15 AM",
                        Amenities = "High-speed Wi-Fi, Executive AC Chair Car, Meal Included",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Ahmedabad",
                        Mode = "Bus",
                        CarrierName = "GSRTC Volvo Air-Conditioned Seater",
                        BasePricePerPassenger = 320m,
                        DurationMinutes = 240, // 4h
                        DistanceKm = 220,
                        AvailabilityScore = 98.0,
                        ComfortScore = 78.0,
                        EcoRating = 4.0,
                        DepartureTime = "Every 30 Mins",
                        ArrivalTime = "4h after departure",
                        Amenities = "Reclining Seats, AC, Water Bottle",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Ahmedabad",
                        Mode = "Car",
                        CarrierName = "Express Highway Private Sedan",
                        BasePricePerPassenger = 1200m,
                        DurationMinutes = 210, // 3h 30m
                        DistanceKm = 220,
                        AvailabilityScore = 85.0,
                        ComfortScore = 88.0,
                        EcoRating = 3.2,
                        DepartureTime = "Flexible",
                        ArrivalTime = "3.5h after departure",
                        Amenities = "Express Toll Passage, AC, Flexible Stops",
                        IsActive = true,
                        IsEstimatedData = true
                    },

                    // Ahmedabad -> Delhi (~930 km)
                    new Models.TransportOption
                    {
                        Source = "Ahmedabad",
                        Destination = "Delhi",
                        Mode = "Train",
                        CarrierName = "Rajdhani Express Superfast",
                        BasePricePerPassenger = 1850m,
                        DurationMinutes = 810, // 13h 30m
                        DistanceKm = 930,
                        AvailabilityScore = 88.0,
                        ComfortScore = 90.0,
                        EcoRating = 4.7,
                        DepartureTime = "05:40 PM",
                        ArrivalTime = "07:10 AM",
                        Amenities = "Complimentary Dinner & Breakfast, Clean Bedding, AC 2-Tier",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Ahmedabad",
                        Destination = "Delhi",
                        Mode = "Flight",
                        CarrierName = "IndiGo Non-stop",
                        BasePricePerPassenger = 3800m,
                        DurationMinutes = 105, // 1h 45m
                        DistanceKm = 930,
                        AvailabilityScore = 92.0,
                        ComfortScore = 88.0,
                        EcoRating = 2.8,
                        DepartureTime = "09:20 AM",
                        ArrivalTime = "11:05 AM",
                        Amenities = "Priority Check-in, Web Boarding, Onboard Snack",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Ahmedabad",
                        Destination = "Delhi",
                        Mode = "Bus",
                        CarrierName = "Mahasagar Travels Multi-Axle Sleeper",
                        BasePricePerPassenger = 1400m,
                        DurationMinutes = 960, // 16h
                        DistanceKm = 930,
                        AvailabilityScore = 70.0,
                        ComfortScore = 72.0,
                        EcoRating = 3.6,
                        DepartureTime = "04:00 PM",
                        ArrivalTime = "08:00 AM",
                        Amenities = "Double Sleeper Berth, Personal TV Screen, AC",
                        IsActive = true,
                        IsEstimatedData = true
                    },

                    // Mumbai -> Delhi (~1400 km)
                    new Models.TransportOption
                    {
                        Source = "Mumbai",
                        Destination = "Delhi",
                        Mode = "Flight",
                        CarrierName = "Vistara Airways Premium Economy",
                        BasePricePerPassenger = 5200m,
                        DurationMinutes = 130, // 2h 10m
                        DistanceKm = 1400,
                        AvailabilityScore = 96.0,
                        ComfortScore = 96.0,
                        EcoRating = 2.7,
                        DepartureTime = "07:00 AM",
                        ArrivalTime = "09:10 AM",
                        Amenities = "Gourmet Hot Meal, Extra Legroom, High Baggage Allowance",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Mumbai",
                        Destination = "Delhi",
                        Mode = "Train",
                        CarrierName = "August Kranti Rajdhani Express",
                        BasePricePerPassenger = 2400m,
                        DurationMinutes = 930, // 15h 30m
                        DistanceKm = 1400,
                        AvailabilityScore = 84.0,
                        ComfortScore = 91.0,
                        EcoRating = 4.8,
                        DepartureTime = "05:10 PM",
                        ArrivalTime = "08:40 AM",
                        Amenities = "Full Catering Service, High-speed Rail, AC First/2-Tier",
                        IsActive = true,
                        IsEstimatedData = true
                    },

                    // Rajkot -> Delhi (~1100 km)
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Delhi",
                        Mode = "Train",
                        CarrierName = "Rajkot - Delhi Sarai Rohilla Weekly Express",
                        BasePricePerPassenger = 1450m,
                        DurationMinutes = 1140, // 19h
                        DistanceKm = 1100,
                        AvailabilityScore = 78.0,
                        ComfortScore = 80.0,
                        EcoRating = 4.6,
                        DepartureTime = "01:00 PM",
                        ArrivalTime = "08:00 AM",
                        Amenities = "Pantry Car, Sleeping Berth, AC 3-Tier",
                        IsActive = true,
                        IsEstimatedData = true
                    },
                    new Models.TransportOption
                    {
                        Source = "Rajkot",
                        Destination = "Delhi",
                        Mode = "Flight",
                        CarrierName = "IndiGo Connecting via Mumbai",
                        BasePricePerPassenger = 6200m,
                        DurationMinutes = 270, // 4h 30m
                        DistanceKm = 1100,
                        AvailabilityScore = 80.0,
                        ComfortScore = 85.0,
                        EcoRating = 2.4,
                        DepartureTime = "11:45 AM",
                        ArrivalTime = "04:15 PM",
                        Amenities = "Single PNR Baggage Transfer, In-flight Beverage",
                        IsActive = true,
                        IsEstimatedData = true
                    }
                };

                await context.TransportOptions.AddRangeAsync(sampleOptions);
                await context.SaveChangesAsync();
            }

            // 4. Demo Chatbot FAQs
            if (!await context.ChatbotFAQs.AnyAsync())
            {
                var faqs = new List<Models.ChatbotFAQ>
                {
                    new Models.ChatbotFAQ
                    {
                        Question = "How does Trip Analyzer work?",
                        Answer = "Trip Analyzer evaluates Train, Bus, Flight, and Driving options for your journey. It calculates a weighted Recommendation Score (0-100) combining Cost (40%), Duration (30%), Availability (20%), and Onboard Comfort (10%) to find your optimal travel mode.",
                        Category = "General",
                        Keywords = "how,work,function,algorithm,recommendation,system",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new Models.ChatbotFAQ
                    {
                        Question = "Which transport is cheapest?",
                        Answer = "When searching a route, click the 'Cheapest' filter tab on the results screen. Trains and government bus services (like GSRTC) are typically the most budget-friendly options for Indian routes.",
                        Category = "Pricing",
                        Keywords = "cheap,budget,lowest,price,cost,fare",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new Models.ChatbotFAQ
                    {
                        Question = "How is the recommendation score calculated?",
                        Answer = "Our proprietary recommendation engine normalizes real-time route options: Cost accounts for 40% of the score, Travel Duration accounts for 30%, Seat Availability contributes 20%, and Onboard Comfort rating contributes 10%.",
                        Category = "Recommendation",
                        Keywords = "score,calculate,formula,weight,rating,recommendation",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new Models.ChatbotFAQ
                    {
                        Question = "Is the ticket data live or estimated?",
                        Answer = "Options labeled 'Estimated Data' reflect benchmark sample fares and schedules. Trip Analyzer is designed to integrate live APIs (such as IRCTC, Amadeus, or RedBus) seamlessly as official keys are connected.",
                        Category = "General",
                        Keywords = "live,estimated,sample,api,realtime,booking",
                        DisplayOrder = 4,
                        IsActive = true
                    },
                    new Models.ChatbotFAQ
                    {
                        Question = "How can I change my password?",
                        Answer = "Log into your account, click on your profile avatar in the navigation bar, and select 'Profile'. You will find a 'Change Password' option under account security.",
                        Category = "Account",
                        Keywords = "password,reset,account,profile,change,security",
                        DisplayOrder = 5,
                        IsActive = true
                    },
                    new Models.ChatbotFAQ
                    {
                        Question = "How can I contact support?",
                        Answer = "Navigate to the 'Contact' page in the menu or footer. Fill out the contact form with your query, and our support team will reply directly to your email address.",
                        Category = "Support",
                        Keywords = "contact,support,help,email,enquiry,admin",
                        DisplayOrder = 6,
                        IsActive = true
                    }
                };

                await context.ChatbotFAQs.AddRangeAsync(faqs);
                await context.SaveChangesAsync();
            }
            // 5. Clean up duplicate or obsolete districts from previous seed models
            var obsoleteDistricts = await context.Districts
                .Where(d => string.IsNullOrEmpty(d.StateCode) || string.IsNullOrEmpty(d.DistrictCode))
                .ToListAsync();

            if (obsoleteDistricts.Any())
            {
                context.Districts.RemoveRange(obsoleteDistricts);
                await context.SaveChangesAsync();
            }

            // Initialize counters
            int districtsProcessed = 0;
            int districtsInserted = 0;
            int districtsUpdated = 0;
            int citiesProcessed = 0;
            int citiesInserted = 0;
            int citiesUpdated = 0;
            int duplicatesSkipped = 0;

            // Load existing Districts into memory
            var districts = await context.Districts.ToListAsync();
            var districtsDict = new Dictionary<(string StateCode, string DistrictCode), Models.District>();
            foreach (var d in districts)
            {
                var key = (d.StateCode ?? "", d.DistrictCode ?? "");
                districtsDict[key] = d;
            }

            // Local helper to locate seed files on disk
            string FindFile(string filename)
            {
                var paths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", filename),
                    Path.Combine(AppContext.BaseDirectory, "Data", "Seed", filename),
                    Path.Combine(Directory.GetCurrentDirectory(), filename),
                    Path.Combine(AppContext.BaseDirectory, filename)
                };
                foreach (var p in paths)
                {
                    if (File.Exists(p)) return p;
                }
                throw new FileNotFoundException($"Seed file {filename} not found.");
            }

            // Read districts dataset
            var districtsFilePath = FindFile("IndiaDistricts.csv");
            var districtLines = await File.ReadAllLinesAsync(districtsFilePath);

            for (int i = 1; i < districtLines.Length; i++)
            {
                var line = districtLines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                districtsProcessed++;

                var parts = line.Split(',');
                if (parts.Length < 6)
                {
                    continue;
                }

                var state = parts[0].Trim();
                var stateCode = parts[1].Trim();
                var districtName = parts[2].Trim();
                var districtCode = parts[3].Trim();

                if (!double.TryParse(parts[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(parts[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    continue;
                }

                if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                {
                    continue;
                }

                var key = (stateCode, districtCode);
                if (districtsDict.TryGetValue(key, out var existingDistrict))
                {
                    bool changed = existingDistrict.Name != districtName ||
                                   existingDistrict.State != state ||
                                   Math.Abs(existingDistrict.Latitude - lat) > 1e-9 ||
                                   Math.Abs(existingDistrict.Longitude - lon) > 1e-9 ||
                                   existingDistrict.IsActive != true;

                    if (changed)
                    {
                        existingDistrict.Name = districtName;
                        existingDistrict.State = state;
                        existingDistrict.Latitude = lat;
                        existingDistrict.Longitude = lon;
                        existingDistrict.IsActive = true;
                        districtsUpdated++;
                    }
                    else
                    {
                        duplicatesSkipped++;
                    }
                }
                else
                {
                    var newDistrict = new Models.District
                    {
                        Name = districtName,
                        State = state,
                        StateCode = stateCode,
                        DistrictCode = districtCode,
                        Latitude = lat,
                        Longitude = lon,
                        IsActive = true
                    };
                    context.Districts.Add(newDistrict);
                    districtsDict[key] = newDistrict;
                    districtsInserted++;
                }
            }

            // 6. Global/International Locations Seeding (Separate from Indian LGD dataset) - Districts
            var globalDistricts = new List<Models.District>
            {
                new Models.District { Name = "New York", State = "New York", StateCode = "NY", DistrictCode = "NYC", Latitude = 40.7128, Longitude = -74.0060 },
                new Models.District { Name = "London", State = "England", StateCode = "ENG", DistrictCode = "LDN", Latitude = 51.5074, Longitude = -0.1278 },
                new Models.District { Name = "Tokyo", State = "Tokyo", StateCode = "TYO", DistrictCode = "HND", Latitude = 35.6762, Longitude = 139.6503 },
                new Models.District { Name = "Dubai", State = "Dubai", StateCode = "DXB", DistrictCode = "DXB", Latitude = 25.2048, Longitude = 55.2708 },
                new Models.District { Name = "Paris", State = "Île-de-France", StateCode = "IDF", DistrictCode = "CDG", Latitude = 48.8566, Longitude = 2.3522 }
            };

            foreach (var gd in globalDistricts)
            {
                districtsProcessed++;
                var key = (gd.StateCode ?? "", gd.DistrictCode ?? "");
                if (districtsDict.TryGetValue(key, out var existingGd))
                {
                    bool changed = existingGd.Name != gd.Name ||
                                   existingGd.State != gd.State ||
                                   Math.Abs(existingGd.Latitude - gd.Latitude) > 1e-9 ||
                                   Math.Abs(existingGd.Longitude - gd.Longitude) > 1e-9 ||
                                   existingGd.IsActive != true;
                    if (changed)
                    {
                        existingGd.Name = gd.Name;
                        existingGd.State = gd.State;
                        existingGd.Latitude = gd.Latitude;
                        existingGd.Longitude = gd.Longitude;
                        existingGd.IsActive = true;
                        districtsUpdated++;
                    }
                    else
                    {
                        duplicatesSkipped++;
                    }
                }
                else
                {
                    context.Districts.Add(gd);
                    districtsDict[key] = gd;
                    districtsInserted++;
                }
            }

            // Save districts first to generate IDs
            await context.SaveChangesAsync();

            // Load existing Cities into memory
            var cities = await context.Cities.ToListAsync();
            var citiesDict = new Dictionary<(int DistrictId, string CityName), Models.City>();
            foreach (var c in cities)
            {
                var key = (c.DistrictId, (c.Name ?? "").Trim().ToLowerInvariant());
                citiesDict[key] = c;
            }

            // Read cities dataset
            var citiesFilePath = FindFile("IndiaCities.csv");
            var cityLines = await File.ReadAllLinesAsync(citiesFilePath);

            for (int i = 1; i < cityLines.Length; i++)
            {
                var line = cityLines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                citiesProcessed++;

                var parts = line.Split(',');
                if (parts.Length < 7)
                {
                    continue;
                }

                var cityName = parts[0].Trim();
                var state = parts[1].Trim();
                var stateCode = parts[2].Trim();
                var districtCode = parts[3].Trim();
                var cityCode = parts[4].Trim();

                if (!double.TryParse(parts[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(parts[6].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    continue;
                }

                if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                {
                    continue;
                }

                var districtKey = (stateCode, districtCode);
                if (!districtsDict.TryGetValue(districtKey, out var parentDistrict))
                {
                    continue;
                }

                var cityKey = (parentDistrict.Id, cityName.ToLowerInvariant());
                if (citiesDict.TryGetValue(cityKey, out var existingCity))
                {
                    bool changed = existingCity.State != state ||
                                   existingCity.StateCode != stateCode ||
                                   existingCity.CityCode != cityCode ||
                                   Math.Abs(existingCity.Latitude - lat) > 1e-9 ||
                                   Math.Abs(existingCity.Longitude - lon) > 1e-9 ||
                                   existingCity.IsActive != true;
                    if (changed)
                    {
                        existingCity.State = state;
                        existingCity.StateCode = stateCode;
                        existingCity.CityCode = cityCode;
                        existingCity.Latitude = lat;
                        existingCity.Longitude = lon;
                        existingCity.IsActive = true;
                        citiesUpdated++;
                    }
                    else
                    {
                        duplicatesSkipped++;
                    }
                }
                else
                {
                    var newCity = new Models.City
                    {
                        Name = cityName,
                        State = state,
                        StateCode = stateCode,
                        DistrictId = parentDistrict.Id,
                        CityCode = cityCode,
                        Latitude = lat,
                        Longitude = lon,
                        IsActive = true
                    };
                    context.Cities.Add(newCity);
                    citiesDict[cityKey] = newCity;
                    citiesInserted++;
                }
            }

            // Global/International Cities Seeding
            var globalCities = new List<(string Name, string State, string StateCode, string DistrictCode, string CityCode, double Lat, double Lng)>
            {
                ("New York", "New York", "NY", "NYC", "C-US-NYC", 40.7128, -74.0060),
                ("London", "England", "ENG", "LDN", "C-EG-LDN", 51.5074, -0.1278),
                ("Tokyo", "Tokyo", "TYO", "HND", "C-JP-HND", 35.6762, 139.6503),
                ("Dubai", "Dubai", "DXB", "DXB", "C-UA-DXB", 25.2048, 55.2708),
                ("Paris", "Île-de-France", "IDF", "CDG", "C-FR-CDG", 48.8566, 2.3522)
            };

            foreach (var gc in globalCities)
            {
                citiesProcessed++;
                var districtKey = (gc.StateCode, gc.DistrictCode);
                if (districtsDict.TryGetValue(districtKey, out var parentGlobalDistrict))
                {
                    var cityKey = (parentGlobalDistrict.Id, gc.Name.Trim().ToLowerInvariant());
                    if (citiesDict.TryGetValue(cityKey, out var existingGc))
                    {
                        bool changed = existingGc.State != gc.State ||
                                       existingGc.StateCode != gc.StateCode ||
                                       existingGc.CityCode != gc.CityCode ||
                                       Math.Abs(existingGc.Latitude - gc.Lat) > 1e-9 ||
                                       Math.Abs(existingGc.Longitude - gc.Lng) > 1e-9 ||
                                       existingGc.IsActive != true;
                        if (changed)
                        {
                            existingGc.State = gc.State;
                            existingGc.StateCode = gc.StateCode;
                            existingGc.CityCode = gc.CityCode;
                            existingGc.Latitude = gc.Lat;
                            existingGc.Longitude = gc.Lng;
                            existingGc.IsActive = true;
                            citiesUpdated++;
                        }
                        else
                        {
                            duplicatesSkipped++;
                        }
                    }
                    else
                    {
                        var newCity = new Models.City
                        {
                            Name = gc.Name,
                            State = gc.State,
                            StateCode = gc.StateCode,
                            DistrictId = parentGlobalDistrict.Id,
                            CityCode = gc.CityCode,
                            Latitude = gc.Lat,
                            Longitude = gc.Lng,
                            IsActive = true
                        };
                        context.Cities.Add(newCity);
                        citiesDict[cityKey] = newCity;
                        citiesInserted++;
                    }
                }
            }

            // Save cities
            await context.SaveChangesAsync();

            // Print summary
            Console.WriteLine();
            Console.WriteLine($"Districts processed: {districtsProcessed}");
            Console.WriteLine($"Districts inserted: {districtsInserted}");
            Console.WriteLine($"Districts updated: {districtsUpdated}");
            Console.WriteLine($"Cities processed: {citiesProcessed}");
            Console.WriteLine($"Cities inserted: {citiesInserted}");
            Console.WriteLine($"Cities updated: {citiesUpdated}");
            Console.WriteLine($"Duplicates skipped: {duplicatesSkipped}");
            Console.WriteLine();

            // 7. Fare Configurations
            var existingFares = await context.FareConfigurations.ToListAsync();
            var requiredFares = new List<Models.FareConfiguration>
            {
                new Models.FareConfiguration { Mode = "Train", BaseFare = 100m, PerKmRate = 1.3m, MinimumFare = 250m },
                new Models.FareConfiguration { Mode = "Bus", BaseFare = 150m, PerKmRate = 1.5m, MinimumFare = 300m },
                new Models.FareConfiguration { Mode = "Flight", BaseFare = 1500m, PerKmRate = 4.8m, MinimumFare = 2500m },
                new Models.FareConfiguration { Mode = "Car", BaseFare = 200m, PerKmRate = 3.2m, MinimumFare = 600m }
            };

            foreach (var req in requiredFares)
            {
                var match = existingFares.FirstOrDefault(f => f.Mode.Equals(req.Mode, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    context.FareConfigurations.Add(req);
                }
                else
                {
                    if (match.BaseFare != req.BaseFare || match.PerKmRate != req.PerKmRate || match.MinimumFare != req.MinimumFare)
                    {
                        match.BaseFare = req.BaseFare;
                        match.PerKmRate = req.PerKmRate;
                        match.MinimumFare = req.MinimumFare;
                        match.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
