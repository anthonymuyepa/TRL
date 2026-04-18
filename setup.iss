[Setup]
AppName=TRL
AppVersion=1.0.0
AppPublisher=Shipping Technologies
DefaultDirName={autopf}\ShippingLabelManager
DefaultGroupName=TRL
OutputBaseFilename=ShippingLabelManager_Setup_v1.0.0
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\ShippingLabelManager\publish\ShippingLabelManager.exe"; \
  DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\TRL"; Filename: "{app}\ShippingLabelManager.exe"
Name: "{commondesktop}\TRL"; Filename: "{app}\ShippingLabelManager.exe"

[Run]
Filename: "{app}\ShippingLabelManager.exe"; \
  Description: "Launch TRL"; \
  Flags: nowait postinstall skipifsilent
