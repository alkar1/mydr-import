import pandas as pd
import argparse
import os
import logging

logging.basicConfig(filename='converter.log', level=logging.DEBUG,
                    format='%(asctime)s %(levelname)s %(name)s %(message)s')
logger=logging.getLogger(__name__)

def convert_xls_to_csv(input_filepath):
    try:
        if not input_filepath.lower().endswith(".xls"):
            print(f"Błąd: Plik wejściowy '{input_filepath}' nie ma rozszerzenia .xls.")
            return

        base_name = os.path.splitext(input_filepath)[0]
        output_filepath = base_name + ".csv"

        # Wczytaj plik XLS
        xls_file = pd.ExcelFile(input_filepath)

        # Wczytaj pierwszy arkusz (lub określ nazwę arkusza, jeśli jest inna)
        df = xls_file.parse(xls_file.sheet_names[0])

        # Zapisz do pliku CSV z separatorem w postaci średnika
        df.to_csv(output_filepath, sep=';', index=False, encoding='utf-8')

        print(f"Konwersja zakończona pomyślnie. Plik {output_filepath} został utworzony.")

    except FileNotFoundError:
        print(f"Błąd: Plik {input_filepath} nie został znaleziony.")
        logger.error(f"File not found: {input_filepath}")
    except Exception as e:
        print(f"Wystąpił nieoczekiwany błąd podczas przetwarzania pliku {input_filepath}: {e}")
        logger.error(f"An unexpected error occurred while processing {input_filepath}: {e}", exc_info=True)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Konwertuje plik XLS do CSV z użyciem średnika jako separatora.")
    parser.add_argument("input_file", help="Ścieżka do wejściowego pliku XLS.")
    
    args = parser.parse_args()
    
    convert_xls_to_csv(args.input_file)