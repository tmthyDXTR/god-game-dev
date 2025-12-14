#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

// This script works when the repository root is either the project root
// (where `Assets/docs` exists) or the `Assets/` folder itself (where `docs` exists).
const candidates = [
  path.join(process.cwd(),'..', 'Assets', 'docs'),
  path.join(process.cwd(), '..', 'docs')
];

let docsDir = null;
for (const c of candidates) {
  if (fs.existsSync(c) && fs.statSync(c).isDirectory()) {
    docsDir = c;
    break;
  }
}

if (!docsDir) {
  console.error('Docs directory not found. Checked:', candidates.join(', '));
  process.exit(1);
}

const files = fs.readdirSync(docsDir).filter(f => f.endsWith('.md')).sort();
if (files.length === 0) {
  console.log('No markdown files found in', docsDir);
  process.exit(0);
}

// CLI flags: --show (print generated nav), --force (write even if identical), --dry-run (no writes)
const args = process.argv.slice(2);
const optShow = args.includes('--show');
const optForce = args.includes('--force');
const optDryRun = args.includes('--dry-run') || args.includes('--dryrun');

console.log('Docs generator: using docs directory:', docsDir);
console.log('Options: show=', optShow, 'force=', optForce, 'dry-run=', optDryRun);
// Determine which script path was used (helpful for CI logs)
try {
  const scriptPath = fs.realpathSync(__filename);
  console.log('Docs generator: running script:', scriptPath);
} catch (e) {
  // ignore
}

function humanize(name) {
//   if (name === 'index') return 'Home';
  return name.replace(/[-_]/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
}

const navItems = files.map(f => {
  const name = path.basename(f, '.md');
  return { title: humanize(name), href: name + '.html' };
});

const startMarker = '<!-- AUTO_NAV_START -->';
const endMarker = '<!-- AUTO_NAV_END -->';

const navLines = [];
navLines.push(startMarker);
navLines.push('<nav>');
navLines.push('<ul style="list-style:none; padding:0; display:flex; gap:1rem;">');
navItems.forEach(item => {
  navLines.push(`  <li><a href="${item.href}">${item.title}</a></li>`);
});
navLines.push('</ul>');
navLines.push('</nav>');
navLines.push(endMarker);
const navBlock = navLines.join('\n');

if (optShow) {
  console.log('\n--- Generated nav block ---\n');
  console.log(navBlock);
  console.log('\n--- end nav block ---\n');
}

files.forEach(file => {
  const filePath = path.join(docsDir, file);
  let content = fs.readFileSync(filePath, 'utf8');
  let newContent;
  const hasMarkers = content.includes(startMarker) && content.includes(endMarker);
  const regex = new RegExp(`${startMarker}[\\s\\S]*?${endMarker}`, 'm');

  if (hasMarkers) {
    newContent = content.replace(regex, navBlock);
    if (newContent === content) {
      if (optForce) {
        if (optDryRun) {
          console.log('Would force rewrite (dry-run) for', file);
        } else {
          fs.writeFileSync(filePath, newContent, 'utf8');
          console.log('Forced rewrite (identical content) for', file);
        }
      } else {
        console.log('No change needed for', file);
      }
      return;
    }

    if (optDryRun) {
      console.log('Would update nav in', file);
    } else {
      fs.writeFileSync(filePath, newContent, 'utf8');
      console.log('Updated nav in', file);
    }
  } else {
    newContent = navBlock + '\n\n' + content;
    if (optDryRun) {
      console.log('Would insert nav in', file);
    } else {
      fs.writeFileSync(filePath, newContent, 'utf8');
      console.log('Inserted nav in', file);
    }
  }
});

console.log('Docs nav generation complete at', docsDir);
