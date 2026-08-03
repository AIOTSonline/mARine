#!/bin/bash
set -e
cd "$(dirname "$0")"
OUT_HTML=design.html
OUT_PDF="Procedural_Terrain_Design_and_Novelty.pdf"

cat d1.html > "$OUT_HTML"
for p in d2.html d3.html d4.html d5.html; do [ -f "$p" ] && cat "$p" >> "$OUT_HTML"; done
echo '</body></html>' >> "$OUT_HTML"

"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
  --headless --disable-gpu --no-sandbox --allow-file-access-from-files \
  --no-pdf-header-footer --print-to-pdf-no-header --virtual-time-budget=12000 \
  --print-to-pdf="$PWD/$OUT_PDF" "file://$PWD/$OUT_HTML" 2>&1 | grep -Ei "written|error" || true

./venv/bin/python -c "
data = open('$OUT_PDF','rb').read()
print('pages:', data.count(b'/Type /Page') - data.count(b'/Type /Pages'))
print('size : %.1f KB' % (len(data)/1024))
"
