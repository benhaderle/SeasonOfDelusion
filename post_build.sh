#!/bin/bash
echo "Uploading IPA to App Store Connect..."
if [ ! -f YOUR_AUTH_KEY.p8 ]; then
echo "Key File not found!"
fi
# Assuming you have set API_ISSUER_ID environment variable
if [ -z "$API_ISSUER_ID" ]; then
echo "Error: API_ISSUER_ID is not set"
exit 1
fi
if xcrun altool --upload-app -type ios -f "$UNITY_PLAYER_PATH" --apiKey YOUR_API_KEY --apiIssuer "$API_ISSUER_ID"; then
echo "Upload IPA to App Store Connect finished with success"
else
echo "Upload IPA to App Store Connect failed"
fi