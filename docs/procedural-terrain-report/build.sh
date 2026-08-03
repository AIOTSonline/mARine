#!/bin/bash
# Assemble the report parts, render to PDF via headless Chrome, verify.
set -e
cd "$(dirname "$0")"

OUT_HTML=report.html
OUT_PDF="Procedural_Terrain_Technical_Report.pdf"

cat p1.html > "$OUT_HTML"
for p in p2.html p3.html p4.html p5.html p6.html p7.html; do
  [ -f "$p" ] && cat "$p" >> "$OUT_HTML"
done
echo '</body></html>' >> "$OUT_HTML"

"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
  --headless --disable-gpu --no-sandbox \
  --allow-file-access-from-files \
  --no-pdf-header-footer \
  --print-to-pdf-no-header \
  --virtual-time-budget=12000 \
  --print-to-pdf="$PWD/$OUT_PDF" \
  "file://$PWD/$OUT_HTML" 2>&1 | grep -Ei "written|error" || true

ls -la "$OUT_PDF"
./venv/bin/python -c "
import zlib, re, sys
data = open('$OUT_PDF','rb').read()
print('pages:', data.count(b'/Type /Page') - data.count(b'/Type /Pages'))
print('size : %.1f KB' % (len(data)/1024))
"
