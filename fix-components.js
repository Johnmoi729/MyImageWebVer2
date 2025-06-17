// fix-components.js - Node.js script to remove standalone from all components
// Save this as fix-components.js in your project root and run: node fix-components.js

const fs = require("fs");
const path = require("path");

function removeStandaloneFromFile(filePath) {
  try {
    let content = fs.readFileSync(filePath, "utf8");
    let modified = false;

    // Remove standalone: true and optional comma
    if (content.includes("standalone:")) {
      content = content.replace(/standalone:\s*true,?\s*/g, "");
      modified = true;
    }

    // Remove imports array from @Component decorator (more complex regex)
    const importsRegex = /imports:\s*\[[^\]]*\],?\s*/g;
    if (content.match(importsRegex)) {
      content = content.replace(importsRegex, "");
      modified = true;
    }

    // Clean up any double commas or trailing commas before closing }
    content = content.replace(/,\s*,/g, ",");
    content = content.replace(/,(\s*})/g, "$1");

    if (modified) {
      fs.writeFileSync(filePath, content, "utf8");
      console.log(`✅ Fixed: ${filePath}`);
      return true;
    }
  } catch (error) {
    console.error(`❌ Error processing ${filePath}:`, error.message);
  }
  return false;
}

function findComponentFiles(dir) {
  let componentFiles = [];

  try {
    const files = fs.readdirSync(dir);

    for (const file of files) {
      const fullPath = path.join(dir, file);
      const stat = fs.statSync(fullPath);

      if (stat.isDirectory()) {
        // Recursively search subdirectories
        componentFiles = componentFiles.concat(findComponentFiles(fullPath));
      } else if (file.endsWith(".component.ts")) {
        componentFiles.push(fullPath);
      }
    }
  } catch (error) {
    console.error(`Error reading directory ${dir}:`, error.message);
  }

  return componentFiles;
}

function main() {
  console.log("🔧 Fixing standalone components...\n");

  const srcDir = path.join(__dirname, "src");

  if (!fs.existsSync(srcDir)) {
    console.error(
      "❌ src directory not found. Make sure you run this script from the project root."
    );
    process.exit(1);
  }

  const componentFiles = findComponentFiles(srcDir);
  console.log(`Found ${componentFiles.length} component files:\n`);

  let fixedCount = 0;

  for (const file of componentFiles) {
    if (removeStandaloneFromFile(file)) {
      fixedCount++;
    } else {
      console.log(`⏭️  Skipped: ${file} (no changes needed)`);
    }
  }

  console.log(
    `\n🎉 Done! Fixed ${fixedCount} out of ${componentFiles.length} components.`
  );
  console.log("\nNow run: ng serve");
}

main();

// ALTERNATIVE: Simple batch command for Windows (save as fix-components.bat)
/*
@echo off
echo Fixing standalone components...

for /r src %%f in (*.component.ts) do (
    echo Processing %%f
    powershell -Command "(Get-Content '%%f') -replace 'standalone:\s*true,?\s*', '' -replace 'imports:\s*\[[^\]]*\],?\s*', '' | Set-Content '%%f'"
)

echo Done! Run 'ng serve' to test.
pause
*/

// ALTERNATIVE: One-liner for Command Prompt (Windows)
// for /r src %f in (*.component.ts) do powershell -Command "(Get-Content '%f') -replace 'standalone:\s*true,?\s*', '' | Set-Content '%f'"
