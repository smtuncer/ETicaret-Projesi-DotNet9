namespace ECommerceApp.Services;

public interface IXmlImportService
{
    Task ImportProductsAsync(string xmlUrl, IProgress<int> progress);
    Task ImportAllProductsAsync();
}
