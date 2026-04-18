# TRL (Track Return Labels) — Setup Guide

## What you need
- Windows 10 / 11 / Server 2016–2022
- Visual Studio 2022 (any edition, including Community — free)
- SQL Server Express (free download from Microsoft)
- .NET 6 SDK (installed automatically with Visual Studio 2022)

---

## Step 1 — Install SQL Server Express

1. Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   - Choose **Express** (free)
2. Run the installer → choose **Basic** installation
3. Default instance name will be: `.\SQLEXPRESS`
4. Also download and install **SQL Server Management Studio (SSMS)** from the same page

---

## Step 2 — Create the database

1. Open **SSMS** and connect to `.\SQLEXPRESS` using Windows Authentication
2. Click **New Query**
3. Open the file `SQL\CreateDatabase.sql` from this project folder
4. Paste the contents into the query window and click **Execute** (F5)
5. You should see: `ShippingLabels database created successfully.`

---

## Step 3 — Open and build the app in Visual Studio

1. Open Visual Studio 2022
2. Click **Open a project or solution**
3. Browse to this folder and open `ShippingLabelManager.sln`
4. Wait for NuGet packages to restore (bottom status bar will show progress)
   - Packages needed: `Microsoft.Data.SqlClient`, `Dapper` (auto-downloaded)
5. Press **F5** to build and run, or **Ctrl+Shift+B** to just build

---

## Step 4 — First run

1. The app will launch and attempt to connect to `.\SQLEXPRESS` / `ShippingLabels`
2. If connection succeeds → you'll see the main window
3. If connection fails → a connection dialog appears:
   - Server: `.\SQLEXPRESS` (or `SERVERNAME\SQLEXPRESS` if on a different machine)
   - Database: `ShippingLabels`
   - Use Windows Authentication (recommended)
   - Click **Test connection**, then **Save & connect**

---

## Step 5 — Add your 55 sites

1. Go to the **Manage Sites** tab
2. Click **+ Add site** for each site
3. Fill in:
   - **LIBCODE** — your internal site code (must be unique)
   - **Site name** — display name
   - **SHIP_ADD_1 to 4** — address lines
   - **ZIP-9** — with or without dash (ZIP9 auto-generated)
   - **MID9** — 9-digit Mailer ID
   - **STC** — 2-digit Service Type Code
   - **ChannelAI** — e.g. 420
   - **Start / resume at** — set to 1 for new sites, or the correct number to resume
4. Click **Save site**

---

## Daily use

1. Open the app
2. Go to **Generate Labels** tab
3. Select a site from the dropdown
4. Set quantity
5. Choose label size (2×4 or 4×6)
6. Click:
   - **Export CSV for mail merge** → saves CSV, opens Save dialog, sequence advances
   - **Preview labels** → shows a print preview (uses first 3 labels, does NOT advance sequence)
   - **Print labels** → prints directly to selected printer, sequence advances

The sequence for each site advances automatically and is saved to SQL Express.

---

## Publishing as a standalone .exe

To deploy to other machines without needing Visual Studio:

1. In Visual Studio, right-click the project → **Publish**
2. Choose **Folder**
3. Set **Deployment mode**: `Self-contained`
4. Set **Target runtime**: `win-x64`
5. Click **Publish**
6. Copy the output folder to the target machine — run `ShippingLabelManager.exe`

No .NET runtime install required on the target machine with self-contained publish.

---

## Connection string reference

Default (SQL Express, Windows Auth, same machine):
```
Server=.\SQLEXPRESS;Database=ShippingLabels;Integrated Security=true;TrustServerCertificate=true;
```

SQL Express on a named server:
```
Server=MYSERVER\SQLEXPRESS;Database=ShippingLabels;Integrated Security=true;TrustServerCertificate=true;
```

SQL login (if Windows Auth is disabled):
```
Server=.\SQLEXPRESS;Database=ShippingLabels;User Id=sa;Password=yourpassword;TrustServerCertificate=true;
```

The connection string is saved in `appsettings.json` next to the `.exe`.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| "Cannot connect" on first launch | Check SQL Express is running: Services → SQL Server (SQLEXPRESS) → Start |
| NuGet packages not restoring | Tools → NuGet Package Manager → Manage NuGet Packages → Restore |
| "Database not found" | Re-run `SQL\CreateDatabase.sql` in SSMS |
| Wrong printer selected | Use the Printer dropdown on the Generate tab |
| Need to correct a sequence | Edit the site → change "Start / resume at" to the correct number |
