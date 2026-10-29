# Power BI / Fabric Inventory Portal - Dokumentacja Projektu

## 1. Cel aplikacji

Aplikacja służy do przeglądania i monitorowania obiektów Microsoft Fabric / Power BI:
- Capacities
- Workspaces
- Datasets
- Reports
- Lakehouses
- Warehouses

Główny cel biznesowy:
- dać użytkownikowi prosty widok inwentarza Power BI,
- ograniczyć liczbę wywołań Power BI REST API przez cache SQL,
- umożliwić analizę Event Log (KQL + statystyki + anomaly detection).

## 2. Jak działa aplikacja (skrót)

1. Użytkownik loguje się przez Entra ID.
2. UI czyta dane głównie z cache w SQL.
3. Odświeżenie danych z Fabric/Power BI zapisuje nowy stan do SQL.
4. Brakujące rekordy nie są kasowane twardo, tylko oznaczane `IsActive = false`.
5. Strona Event Log pozwala analizować logi:
- ręcznie wpisując KQL,
- albo przez `AI Mode`, który generuje propozycję KQL z promptu.

## 3. Moduły funkcjonalne

## 3.1 Inwentarz obiektów
- Strony: `Capacities`, `Workspaces`, `Datasets`, `Reports`, `Lakehouses`, `Warehouses`.
- Tabele z filtrowaniem/sortowaniem i przejściem do `Details`.
- Lakehouse/Warehouse details pobierają świeże dane z Fabric REST API dla workspace i wybierają konkretny item po ID.

## 3.2 Cache SQL
- Źródło odczytu dla UI.
- Tabele modelowe: Capacity, Workspace, Dataset, Report, Lakehouse, Warehouse.
- Status rekordu: aktywny/nieaktywny.

## 3.3 Event Log
- `KQL Query` (manualne zapytania).
- `Aggregated Logs` i `Anomaly Detection`.
- `Table Summary`.
- `AI Mode`:
  - użytkownik wpisuje prompt,
  - model generuje propozycję KQL,
  - użytkownik klika `Apply`, żeby przenieść query do głównego pola.

## 3.4 Role i dostęp
- Grupa administracyjna Entra ID: `PowerBiInventoryAdmin`.
- Admin ma rozszerzony dostęp do obiektów i logiki odczytu.
- Dla zwykłych użytkowników działa cache uprawnień per user.

## 4. Architektura techniczna

- Frontend/Server: Blazor Server (`blz0/blz0`).
- Baza: SQL Server + EF Core.
- Integracje:
  - Power BI REST API,
  - Fabric REST API,
  - Kusto (Event Log),
  - Entra ID (auth),
  - LM Studio (AI generowanie KQL).

Statyczne assety:
- Bootstrap ma lokalną kopię w `wwwroot/lib/bootstrap` jako fallback dla środowisk Docker/dev bez stabilnego dostępu do CDN.
- Bootstrap Icons są odtwarzane lokalnie przez `libman.json` do `wwwroot/lib/bootstrap-icons`.

Szczegóły architektury:
- [architecture.md](architecture.md)

## 5. Konfiguracja

Najważniejsze sekcje konfiguracyjne:
- `ConnectionStrings:blz0ContextSql`
- `ApiCredentials:*`
- `AzureAd:*`
- `Kusto:*`
- `Authorization:*`
- `LmStudio:*`

Wymagane delegated scopes:
- Power BI REST: zakresy skonfigurowane po stronie Power BI API i używane przez `.default`.
- Fabric REST: `Workspace.Read.All`.
- Fabric Lakehouse tables: `Lakehouse.Read.All`.
- Fabric Warehouse SQL endpoint: delegated access dla `https://database.windows.net/.default`.

Przykład dev (LM Studio):
- `LmStudio:BaseUrl = http://host.docker.internal:1234`
- `LmStudio:Model = openai/gpt-oss-20b`

## 6. Uruchomienie (dev)

1. Uruchom SQL Server.
2. Ustaw `.env` / `appsettings.Development.json`.
3. Wykonaj migracje EF.
4. Uruchom aplikację.

Komendy i szczegóły są w:
- [README.md](../README.md)

## 7. Utrzymanie i troubleshooting

Typowe problemy:
- 401/403 z Power BI API: brak scope/consent lub zły tryb auth.
- `WorkspaceTypeNotSupported` przy lakehouse/warehouse: workspace nie obsługuje Fabric item API; globalny refresh pomija takie workspace’y.
- Problem certyfikatów HTTPS w kontenerze: użyj poprawnego profilu uruchomienia i ustawień hosta.
- AI Mode nie odpowiada: sprawdź, czy LM Studio działa i czy endpoint jest osiągalny z kontenera.

## 8. Kierunki rozwoju

- Zapisywanie historii prompt -> wygenerowane KQL.
- Walidacja/normalizacja KQL przed uruchomieniem.
- Rozszerzenie dashboardów Event Log o gotowe scenariusze analityczne.
- Dalsza optymalizacja cache per user i limitowanie calli do REST API.
- Opcjonalnie: przejście lakehouse/warehouse details z workspace-list lookup na dedykowane `GET item by id`, jeżeli aplikacja dostanie zgodę na szersze Fabric item scopes.
