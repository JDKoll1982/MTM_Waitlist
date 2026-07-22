using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class FileServiceTests
{
    [TestMethod]
    public void Read_ReturnsDefault_WhenFileIsMissing()
    {
        var service = new FileService();

        var result = service.Read<Dictionary<string, object>>(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), "missing.json");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SaveReadDelete_RoundTripsContent()
    {
        var service = new FileService();
        var folderPath = Path.Combine(Path.GetTempPath(), $"MTM_Waitlist.Tests.{Guid.NewGuid():N}");
        var fileName = "sample.json";

        try
        {
            service.Save(folderPath, fileName, new Dictionary<string, object>
            {
                ["Name"] = "Alpha",
                ["Count"] = 3
            });

            var result = service.Read<Dictionary<string, object>>(folderPath, fileName);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("Name"));
            Assert.AreEqual("Alpha", result["Name"]?.ToString());

            service.Delete(folderPath, fileName);

            Assert.IsNull(service.Read<Dictionary<string, object>>(folderPath, fileName));
        }
        finally
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
        }
    }
}