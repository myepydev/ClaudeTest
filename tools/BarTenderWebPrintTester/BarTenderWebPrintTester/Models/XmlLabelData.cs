namespace BarTenderWebPrintTester.Models;

/// <summary>
/// DTO for the GHS hazard label XML structure.
/// Maps to BarTender NamedSubStrings in the .btw template.
/// </summary>
public class XmlLabelData
{
    // Product identification
    public string ProductIdentifier { get; set; } = "";
    public string InternalCode { get; set; } = "";
    public string BatchLot { get; set; } = "";
    public string NetQuantity { get; set; } = "";

    // Hazard info
    public string SignalWord { get; set; } = "Pericolo";
    public List<string> HazardStatements { get; set; } = new();       // H-codes
    public List<string> PrecautionaryStatements { get; set; } = new(); // P-codes
    public List<string> SupplementalInfo { get; set; } = new();        // EUH-codes

    // GHS Pictograms
    public List<string> Pictograms { get; set; } = new();

    // Supplier
    public string SupplierName { get; set; } = "";
    public string SupplierAddress { get; set; } = "";
    public string SupplierPhone { get; set; } = "";
    public string EmergencyPhone { get; set; } = "";

    // Optional fields
    public string Language { get; set; } = "it";
    public string UFI { get; set; } = "";
    public string UNNumber { get; set; } = "";
}
