cd ~/projects/TRL

cat > README.md << 'EOF'
# TRL - Track Return Labels

A Windows desktop application for managing and printing NLS (National Library Service) return labels and shipping labels.

## Overview
This application helps libraries and organizations manage their label printing workflow for NLS return labels and shipping labels.

## Features

### Return Labels
- Print return labels via Word mail merge
- Configurable calibration/test labels (0-100)
- Batch printing - split large jobs into 1000-label batches
- Automatic serial number tracking
- Configurable serial display (6 or 9 digits)

### Shipping Labels
- Print shipping labels for outbound boxes
- Manage multiple shipping sites
- Active site selection
- Box quantity tracking per site

### Reporting
- Print history with filtering
- KPI dashboard with usage statistics
- Export reports to CSV, Excel, or PDF

## Configuration
| Setting | Range | Default |
|---------|-------|---------|
| Calibration Count | 0-100 | 20 |
| Batch Size | 500-5000 | 1000 |
| Serial Display Digits | 6 or 9 | 6 |
| Word Merge Timeout | 10-120 min | 30 |

## Technology
- .NET 10.0
- Windows Forms
- Microsoft Word Integration (VBScript)
- SQL Server

## Build Instructions
```bash
dotnet build TRL.sln -c Release /p:EnableWindowsTargeting=true