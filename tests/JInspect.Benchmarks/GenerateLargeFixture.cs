using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JInspect.Benchmarks;

public static class GenerateLargeFixture
{
    private const string FixtureName = "benchmark-200mb.json";

    private static readonly string FixtureDir =
        Path.Combine(Path.GetTempPath(), "jinspect-benchmarks");

    public static string FixturePath => Path.Combine(FixtureDir, FixtureName);

    public static void EnsureExists()
    {
        if (File.Exists(FixturePath))
            return;

        Directory.CreateDirectory(FixtureDir);
        Generate(FixturePath, targetSizeMb: 200);
    }

    private static void Generate(string path, int targetSizeMb)
    {
        var targetBytes = (long)targetSizeMb * 1024 * 1024;

        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Hector", "Iris", "Jack", "Karen", "Leo", "Mona", "Nate", "Olivia", "Paul" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas" };
        var cities = new[] { "London", "Manchester", "Birmingham", "Leeds", "Glasgow", "Edinburgh", "Bristol", "Liverpool", "Sheffield", "Newcastle", "Cardiff", "Belfast", "Dublin", "Paris", "Berlin", "Amsterdam" };
        var countries = new[] { "GB", "IE", "FR", "DE", "NL", "ES", "IT", "PT", "SE", "NO", "DK", "FI", "PL", "CZ", "AT", "CH" };
        var currencies = new[] { "GBP", "EUR", "USD", "CHF", "SEK", "NOK", "DKK", "PLN", "CZK", "JPY", "AUD", "CAD" };
        var categories = new[] { "Electronics", "Clothing", "Food & Beverage", "Home & Garden", "Sports", "Books", "Automotive", "Health", "Toys", "Office Supplies", "Pet Supplies", "Music" };
        var statuses = new[] { "active", "pending", "completed", "cancelled", "refunded", "disputed", "archived" };
        var paymentMethods = new[] { "credit_card", "debit_card", "bank_transfer", "paypal", "stripe", "apple_pay", "google_pay", "crypto" };
        var tags = new[] { "priority", "bulk", "wholesale", "retail", "online", "in-store", "subscription", "one-time", "recurring", "promotional", "clearance", "seasonal", "limited-edition", "exclusive", "pre-order", "backorder" };

        writer.WriteStartArray();

        var id = 0;
        while (stream.Position < targetBytes)
        {
            writer.WriteStartObject();

            writer.WriteString("id", Guid.NewGuid().ToString());
            writer.WriteNumber("sequenceNumber", id);
            writer.WriteString("createdAt", DateTimeOffset.UnixEpoch.AddSeconds(RandomNumberGenerator.GetInt32(0, 1_700_000_000)).ToString("O"));
            writer.WriteString("updatedAt", DateTimeOffset.UnixEpoch.AddSeconds(RandomNumberGenerator.GetInt32(0, 1_700_000_000)).ToString("O"));
            writer.WriteString("status", Pick(statuses));
            writer.WriteBoolean("isVerified", RandomNumberGenerator.GetInt32(0, 2) == 1);
            writer.WriteNumber("version", RandomNumberGenerator.GetInt32(1, 50));

            writer.WritePropertyName("customer");
            writer.WriteStartObject();
            {
                writer.WriteString("customerId", Guid.NewGuid().ToString());
                var first = Pick(firstNames);
                var last = Pick(lastNames);
                writer.WriteString("firstName", first);
                writer.WriteString("lastName", last);
                writer.WriteString("email", $"{first.ToLower()}.{last.ToLower()}@example-{RandomNumberGenerator.GetInt32(1, 500)}.com");
                writer.WriteString("phone", $"+44 {RandomNumberGenerator.GetInt32(7000, 7999)} {RandomNumberGenerator.GetInt32(100000, 999999)}");
                writer.WriteBoolean("isActive", RandomNumberGenerator.GetInt32(0, 10) > 1);
                writer.WriteNumber("loyaltyPoints", RandomNumberGenerator.GetInt32(0, 100_000));
                writer.WriteString("tier", Pick(["bronze", "silver", "gold", "platinum", "diamond"]));
                writer.WriteString("registeredAt", DateTimeOffset.UnixEpoch.AddSeconds(RandomNumberGenerator.GetInt32(0, 1_700_000_000)).ToString("O"));

                writer.WritePropertyName("address");
                writer.WriteStartObject();
                {
                    writer.WriteString("line1", $"{RandomNumberGenerator.GetInt32(1, 999)} {Pick(["High Street", "Main Road", "Church Lane", "Station Road", "Park Avenue", "Victoria Road", "King Street", "Queen Street", "Mill Lane", "The Green"])}");
                    if (RandomNumberGenerator.GetInt32(0, 3) > 0)
                        writer.WriteString("line2", $"Flat {RandomNumberGenerator.GetInt32(1, 50)}");
                    writer.WriteString("city", Pick(cities));
                    writer.WriteString("postcode", $"{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{RandomNumberGenerator.GetInt32(1, 99)} {RandomNumberGenerator.GetInt32(1, 9)}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}");
                    writer.WriteString("country", Pick(countries));

                    writer.WritePropertyName("coordinates");
                    writer.WriteStartObject();
                    {
                        writer.WriteNumber("latitude", Math.Round(RandomDouble(-90, 90), 6));
                        writer.WriteNumber("longitude", Math.Round(RandomDouble(-180, 180), 6));
                        writer.WriteNumber("accuracy", Math.Round(RandomDouble(1, 100), 2));
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();

                writer.WritePropertyName("preferences");
                writer.WriteStartObject();
                {
                    writer.WriteBoolean("emailNotifications", RandomNumberGenerator.GetInt32(0, 2) == 1);
                    writer.WriteBoolean("smsNotifications", RandomNumberGenerator.GetInt32(0, 2) == 1);
                    writer.WriteString("language", Pick(["en", "fr", "de", "es", "it", "pt", "nl", "sv"]));
                    writer.WriteString("currency", Pick(currencies));
                    writer.WriteString("timezone", Pick(["Europe/London", "Europe/Paris", "Europe/Berlin", "America/New_York", "America/Chicago", "Asia/Tokyo", "Australia/Sydney"]));
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();

            writer.WritePropertyName("order");
            writer.WriteStartObject();
            {
                writer.WriteString("orderId", Guid.NewGuid().ToString());
                writer.WriteString("reference", $"ORD-{RandomNumberGenerator.GetInt32(100000, 999999)}");
                writer.WriteString("placedAt", DateTimeOffset.UnixEpoch.AddSeconds(RandomNumberGenerator.GetInt32(0, 1_700_000_000)).ToString("O"));
                writer.WriteString("currency", Pick(currencies));
                writer.WriteString("paymentMethod", Pick(paymentMethods));
                writer.WriteBoolean("isPaid", RandomNumberGenerator.GetInt32(0, 10) > 1);

                var lineCount = RandomNumberGenerator.GetInt32(1, 8);
                var subtotal = 0.0;

                writer.WritePropertyName("lineItems");
                writer.WriteStartArray();
                for (var li = 0; li < lineCount; li++)
                {
                    writer.WriteStartObject();
                    {
                        writer.WriteString("sku", $"SKU-{RandomNumberGenerator.GetInt32(10000, 99999)}");
                        writer.WriteString("name", $"{Pick(["Premium", "Standard", "Basic", "Deluxe", "Economy", "Professional"])} {Pick(["Widget", "Gadget", "Tool", "Component", "Module", "Assembly", "Kit", "Pack"])} {Pick(["Pro", "Plus", "Max", "Ultra", "Lite", "Mini"])}");
                        writer.WriteString("category", Pick(categories));
                        var qty = RandomNumberGenerator.GetInt32(1, 20);
                        var unitPrice = Math.Round(RandomDouble(0.99, 999.99), 2);
                        var lineTotal = Math.Round(qty * unitPrice, 2);
                        subtotal += lineTotal;
                        writer.WriteNumber("quantity", qty);
                        writer.WriteNumber("unitPrice", unitPrice);
                        writer.WriteNumber("lineTotal", lineTotal);
                        writer.WriteNumber("taxRate", Pick([0.0, 0.05, 0.10, 0.20]));
                        writer.WriteString("description", GenerateDescription());

                        writer.WritePropertyName("dimensions");
                        writer.WriteStartObject();
                        {
                            writer.WriteNumber("weightKg", Math.Round(RandomDouble(0.01, 50.0), 3));
                            writer.WriteNumber("lengthCm", Math.Round(RandomDouble(1, 200), 1));
                            writer.WriteNumber("widthCm", Math.Round(RandomDouble(1, 200), 1));
                            writer.WriteNumber("heightCm", Math.Round(RandomDouble(1, 200), 1));
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteNumber("subtotal", Math.Round(subtotal, 2));
                var discount = Math.Round(subtotal * RandomDouble(0, 0.3), 2);
                writer.WriteNumber("discount", discount);
                var tax = Math.Round((subtotal - discount) * 0.2, 2);
                writer.WriteNumber("tax", tax);
                writer.WriteNumber("total", Math.Round(subtotal - discount + tax, 2));

                writer.WritePropertyName("shipping");
                writer.WriteStartObject();
                {
                    writer.WriteString("method", Pick(["standard", "express", "next-day", "economy", "click-and-collect"]));
                    writer.WriteNumber("cost", Math.Round(RandomDouble(0, 25), 2));
                    writer.WriteString("estimatedDelivery", DateTimeOffset.UtcNow.AddDays(RandomNumberGenerator.GetInt32(1, 14)).ToString("yyyy-MM-dd"));
                    writer.WriteString("trackingNumber", RandomNumberGenerator.GetInt32(0, 3) > 0 ? $"TRK{RandomNumberGenerator.GetInt32(1000000, 9999999)}" : null);

                    writer.WritePropertyName("address");
                    writer.WriteStartObject();
                    {
                        writer.WriteString("line1", $"{RandomNumberGenerator.GetInt32(1, 999)} {Pick(["Oak Drive", "Elm Street", "Maple Avenue", "Cedar Lane", "Pine Road", "Birch Way", "Willow Close"])}");
                        writer.WriteString("city", Pick(cities));
                        writer.WriteString("postcode", $"{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{RandomNumberGenerator.GetInt32(1, 99)} {RandomNumberGenerator.GetInt32(1, 9)}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}{(char)('A' + RandomNumberGenerator.GetInt32(0, 26))}");
                        writer.WriteString("country", Pick(countries));
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();

            writer.WritePropertyName("tags");
            writer.WriteStartArray();
            var tagCount = RandomNumberGenerator.GetInt32(1, 6);
            var usedTags = new HashSet<string>();
            for (var t = 0; t < tagCount; t++)
            {
                var tag = Pick(tags);
                if (usedTags.Add(tag))
                    writer.WriteStringValue(tag);
            }
            writer.WriteEndArray();

            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            {
                writer.WriteString("source", Pick(["web", "mobile-ios", "mobile-android", "api", "pos", "phone", "email"]));
                writer.WriteString("userAgent", Pick(["Mozilla/5.0 (Windows NT 10.0; Win64; x64)", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)", "Mozilla/5.0 (Linux; Android 14)"]));
                writer.WriteString("ipAddress", $"{RandomNumberGenerator.GetInt32(1, 255)}.{RandomNumberGenerator.GetInt32(0, 255)}.{RandomNumberGenerator.GetInt32(0, 255)}.{RandomNumberGenerator.GetInt32(1, 255)}");
                writer.WriteString("sessionId", Guid.NewGuid().ToString());
                writer.WriteNumber("requestDurationMs", RandomNumberGenerator.GetInt32(10, 5000));

                if (RandomNumberGenerator.GetInt32(0, 3) == 0)
                {
                    writer.WritePropertyName("abTests");
                    writer.WriteStartObject();
                    {
                        writer.WriteString("checkoutFlow", Pick(["control", "variant-a", "variant-b"]));
                        writer.WriteString("pricingDisplay", Pick(["control", "variant-a"]));
                    }
                    writer.WriteEndObject();
                }

                writer.WritePropertyName("audit");
                writer.WriteStartObject();
                {
                    writer.WriteString("createdBy", Guid.NewGuid().ToString());
                    writer.WriteString("lastModifiedBy", Guid.NewGuid().ToString());
                    writer.WriteNumber("changeCount", RandomNumberGenerator.GetInt32(1, 50));
                    writer.WritePropertyName("history");
                    writer.WriteStartArray();
                    var histCount = RandomNumberGenerator.GetInt32(1, 4);
                    for (var h = 0; h < histCount; h++)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("action", Pick(["created", "updated", "status_changed", "payment_received", "shipped", "delivered"]));
                        writer.WriteString("timestamp", DateTimeOffset.UnixEpoch.AddSeconds(RandomNumberGenerator.GetInt32(0, 1_700_000_000)).ToString("O"));
                        writer.WriteString("actor", Guid.NewGuid().ToString());
                        writer.WriteString("detail", GenerateDescription());
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();

            if (RandomNumberGenerator.GetInt32(0, 5) == 0)
                writer.WriteString("notes", GenerateLongText());

            writer.WriteEndObject();
            writer.Flush();

            id++;
        }

        writer.WriteEndArray();
        writer.Flush();

        Console.WriteLine($"Generated {id} records, {stream.Position / (1024.0 * 1024.0):F1} MB");
    }

    private static T Pick<T>(T[] items) =>
        items[RandomNumberGenerator.GetInt32(0, items.Length)];

    private static double RandomDouble(double min, double max)
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        var ratio = (double)BitConverter.ToUInt64(bytes) / ulong.MaxValue;
        return min + ratio * (max - min);
    }

    private static string GenerateDescription()
    {
        var words = new[] { "high-quality", "durable", "lightweight", "premium", "eco-friendly", "handcrafted", "innovative", "versatile", "compact", "ergonomic", "waterproof", "stainless", "adjustable", "portable", "rechargeable", "wireless", "multi-purpose", "heat-resistant", "non-toxic", "biodegradable" };
        var count = RandomNumberGenerator.GetInt32(5, 15);
        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(Pick(words));
        }
        return sb.ToString();
    }

    private static string GenerateLongText()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < RandomNumberGenerator.GetInt32(3, 8); i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(GenerateDescription());
            sb.Append('.');
        }
        return sb.ToString();
    }
}
