"""Pre-built sample hazard labels for testing."""

from .models import HazardLabel, SignalWord, GHSPictogram


def acetone_label() -> HazardLabel:
    """GHS label for Acetone (common solvent)."""
    return HazardLabel(
        product_name="Acetone",
        signal_word=SignalWord.PERICOLO,
        pictograms=[GHSPictogram.GHS02, GHSPictogram.GHS07],
        hazard_statements=[
            "H225 - Liquido e vapori facilmente infiammabili",
            "H319 - Provoca grave irritazione oculare",
            "H336 - Può provocare sonnolenza o vertigini",
        ],
        precautionary_statements=[
            "P210 - Tenere lontano da fonti di calore, superfici calde, scintille, fiamme libere e altre fonti di accensione. Non fumare",
            "P233 - Tenere il recipiente ben chiuso",
            "P240 - Mettere a terra e a massa il contenitore e il dispositivo ricevente",
            "P305+P351+P338 - IN CASO DI CONTATTO CON GLI OCCHI: sciacquare accuratamente per parecchi minuti. Togliere le eventuali lenti a contatto se è agevole farlo. Continuare a sciacquare",
            "P403+P233 - Conservare in luogo ben ventilato. Tenere il recipiente ben chiuso",
        ],
        supplier_name="ChimicaTest S.r.l.",
        supplier_address="Via Roma 1, 20100 Milano (MI), Italia",
        supplier_phone="+39 02 1234567",
        cas_number="67-64-1",
        un_number="UN1090",
        quantity="1 L",
        batch_number="LOT-2026-001",
        date="2026-02-20",
    )


def sodium_hydroxide_label() -> HazardLabel:
    """GHS label for Sodium Hydroxide (caustic soda)."""
    return HazardLabel(
        product_name="Sodio idrossido (NaOH)",
        signal_word=SignalWord.PERICOLO,
        pictograms=[GHSPictogram.GHS05],
        hazard_statements=[
            "H290 - Può essere corrosivo per i metalli",
            "H314 - Provoca gravi ustioni cutanee e gravi lesioni oculari",
        ],
        precautionary_statements=[
            "P260 - Non respirare le polveri",
            "P280 - Indossare guanti/indumenti protettivi/protezione per gli occhi/il viso",
            "P301+P330+P331 - IN CASO DI INGESTIONE: sciacquare la bocca. NON provocare il vomito",
            "P303+P361+P353 - IN CASO DI CONTATTO CON LA PELLE (o con i capelli): togliere immediatamente tutti gli indumenti contaminati. Sciacquare la pelle con acqua",
            "P305+P351+P338 - IN CASO DI CONTATTO CON GLI OCCHI: sciacquare accuratamente per parecchi minuti. Togliere le eventuali lenti a contatto se è agevole farlo. Continuare a sciacquare",
        ],
        supplier_name="ChimicaTest S.r.l.",
        supplier_address="Via Roma 1, 20100 Milano (MI), Italia",
        supplier_phone="+39 02 1234567",
        cas_number="1310-73-2",
        quantity="500 g",
        batch_number="LOT-2026-002",
        date="2026-02-20",
    )


def ethanol_label() -> HazardLabel:
    """GHS label for Ethanol."""
    return HazardLabel(
        product_name="Etanolo (C2H5OH)",
        signal_word=SignalWord.PERICOLO,
        pictograms=[GHSPictogram.GHS02],
        hazard_statements=[
            "H225 - Liquido e vapori facilmente infiammabili",
        ],
        precautionary_statements=[
            "P210 - Tenere lontano da fonti di calore, superfici calde, scintille, fiamme libere e altre fonti di accensione. Non fumare",
            "P233 - Tenere il recipiente ben chiuso",
            "P240 - Mettere a terra e a massa il contenitore e il dispositivo ricevente",
            "P241 - Utilizzare impianti elettrici/di ventilazione/di illuminazione a prova di esplosione",
            "P303+P361+P353 - IN CASO DI CONTATTO CON LA PELLE (o con i capelli): togliere immediatamente tutti gli indumenti contaminati. Sciacquare la pelle con acqua",
        ],
        supplier_name="ChimicaTest S.r.l.",
        supplier_address="Via Roma 1, 20100 Milano (MI), Italia",
        supplier_phone="+39 02 1234567",
        cas_number="64-17-5",
        un_number="UN1170",
        quantity="2.5 L",
        batch_number="LOT-2026-003",
        date="2026-02-20",
    )


def hydrogen_peroxide_label() -> HazardLabel:
    """GHS label for Hydrogen Peroxide 30%."""
    return HazardLabel(
        product_name="Perossido di idrogeno 30% (H2O2)",
        signal_word=SignalWord.PERICOLO,
        pictograms=[GHSPictogram.GHS03, GHSPictogram.GHS05, GHSPictogram.GHS07],
        hazard_statements=[
            "H271 - Può provocare un incendio o un'esplosione; molto comburente",
            "H302 - Nocivo se ingerito",
            "H314 - Provoca gravi ustioni cutanee e gravi lesioni oculari",
            "H332 - Nocivo se inalato",
        ],
        precautionary_statements=[
            "P220 - Tenere lontano da indumenti e altri materiali combustibili",
            "P280 - Indossare guanti/indumenti protettivi/protezione per gli occhi/il viso",
            "P305+P351+P338 - IN CASO DI CONTATTO CON GLI OCCHI: sciacquare accuratamente per parecchi minuti",
            "P310 - Contattare immediatamente un CENTRO ANTIVELENI o un medico",
        ],
        supplier_name="ChimicaTest S.r.l.",
        supplier_address="Via Roma 1, 20100 Milano (MI), Italia",
        supplier_phone="+39 02 1234567",
        cas_number="7722-84-1",
        un_number="UN2014",
        quantity="1 L",
        batch_number="LOT-2026-004",
        date="2026-02-20",
        copies=2,
    )


SAMPLE_LABELS = {
    "acetone": acetone_label,
    "naoh": sodium_hydroxide_label,
    "ethanol": ethanol_label,
    "h2o2": hydrogen_peroxide_label,
}
