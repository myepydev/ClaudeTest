using System.Xml.Linq;
using BarTenderWebPrintTester.Models;

namespace BarTenderWebPrintTester.Services;

/// <summary>
/// Generates XML for GHS hazard labels, compatible with BarTender NamedSubString mapping.
/// </summary>
public class XmlGeneratorService
{
    /// <summary>
    /// Generate a complete GHS hazard label XML from label data.
    /// </summary>
    public string GenerateXml(XmlLabelData data)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("HazardLabel",
                new XAttribute("Version", "1.0"),
                new XAttribute("Language", data.Language),

                new XElement("Product",
                    new XElement("ProductIdentifier", data.ProductIdentifier),
                    new XElement("InternalCode", data.InternalCode),
                    new XElement("BatchLot", data.BatchLot),
                    new XElement("NetQuantity", data.NetQuantity)
                ),

                new XElement("Hazard",
                    new XElement("SignalWord", data.SignalWord),
                    new XElement("HazardStatements",
                        data.HazardStatements.Select(h => new XElement("Statement", h))
                    ),
                    new XElement("PrecautionaryStatements",
                        data.PrecautionaryStatements.Select(p => new XElement("Statement", p))
                    ),
                    data.SupplementalInfo.Count > 0
                        ? new XElement("SupplementalInfo",
                            data.SupplementalInfo.Select(s => new XElement("Statement", s)))
                        : null
                ),

                new XElement("Pictograms",
                    data.Pictograms.Select(p => new XElement("Pictogram", p))
                ),

                new XElement("Supplier",
                    new XElement("Name", data.SupplierName),
                    new XElement("Address", data.SupplierAddress),
                    new XElement("Phone", data.SupplierPhone),
                    !string.IsNullOrEmpty(data.EmergencyPhone)
                        ? new XElement("EmergencyPhone", data.EmergencyPhone)
                        : null
                ),

                !string.IsNullOrEmpty(data.UFI)
                    ? new XElement("UFI", data.UFI)
                    : null,
                !string.IsNullOrEmpty(data.UNNumber)
                    ? new XElement("UNNumber", data.UNNumber)
                    : null
            )
        );

        return doc.Declaration + Environment.NewLine + doc.Root;
    }

    /// <summary>
    /// Generate a sample label with realistic data for testing.
    /// </summary>
    public XmlLabelData GenerateSampleData()
    {
        return new XmlLabelData
        {
            ProductIdentifier = "Acetone",
            InternalCode = "CHM-ACE-001",
            BatchLot = "LOT-2026-0042",
            NetQuantity = "1 L",
            SignalWord = "Pericolo",
            HazardStatements = new List<string>
            {
                "H225 - Liquido e vapori facilmente infiammabili",
                "H319 - Provoca grave irritazione oculare",
                "H336 - Può provocare sonnolenza o vertigini"
            },
            PrecautionaryStatements = new List<string>
            {
                "P210 - Tenere lontano da fonti di calore, superfici calde, scintille, fiamme libere e altre fonti di accensione. Non fumare",
                "P233 - Tenere il recipiente ben chiuso",
                "P305+P351+P338 - IN CASO DI CONTATTO CON GLI OCCHI: sciacquare accuratamente per parecchi minuti"
            },
            SupplementalInfo = new List<string>(),
            Pictograms = new List<string> { "GHS02", "GHS07" },
            SupplierName = "ChimicaTest S.r.l.",
            SupplierAddress = "Via Roma 1, 20100 Milano (MI), Italia",
            SupplierPhone = "+39 02 1234567",
            EmergencyPhone = "+39 02 7654321",
            Language = "it",
            UFI = "N1QV-10GN-J00G-XXXX",
            UNNumber = "UN1090"
        };
    }

    /// <summary>
    /// Validate minimum required fields for a label.
    /// </summary>
    public List<string> Validate(XmlLabelData data)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.ProductIdentifier))
            errors.Add("ProductIdentifier è obbligatorio");
        if (string.IsNullOrWhiteSpace(data.SignalWord))
            errors.Add("SignalWord è obbligatorio (Pericolo o Attenzione)");
        if (data.HazardStatements.Count == 0)
            errors.Add("Almeno un'indicazione di pericolo (H-code) è obbligatoria");
        if (data.Pictograms.Count == 0)
            errors.Add("Almeno un pittogramma GHS è obbligatorio");
        if (string.IsNullOrWhiteSpace(data.SupplierName))
            errors.Add("Nome fornitore è obbligatorio");

        return errors;
    }
}
