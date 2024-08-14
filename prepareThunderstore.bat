@echo off
:: LICENSE.md isn't in a prominent place for viewers of Thunderstore, but it is included in the folder where
:: it extracted to when auto-installed through a mod manager (Thunderstore or r2modman), so it can still be useful
:: to include it in the Thunderstore zip.
echo Once this is run and plugin DLL is in .thunderstore folder, you can simply zip the contents and upload to Thunderstore
pause
xcopy /V/Y "LICENSE.md" /I ".thunderstore\LICENSE.md"*
xcopy /V/Y "CHANGELOG.md" /I ".thunderstore\CHANGELOG.md"*
xcopy /V/Y "README.md" /I ".thunderstore\README.md"*