import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';

const script = readFileSync(new URL('../../scripts/localdb-backup-restore.ps1', import.meta.url), 'utf8');

test('LocalDB backup/restore rehearsal is explicit and non-destructive by default', () => {
  for (const marker of ['sqlcmd', 'BACKUP DATABASE', 'RESTORE VERIFYONLY', 'RESTORE DATABASE', 'DBCC CHECKDB'])
    assert.match(script, new RegExp(marker.replaceAll(' ', '\\s+'), 'i'));
  assert.match(script, /RestoreDatabase/);
  assert.match(script, /USE \$restoreIdentifier/);
  assert.doesNotMatch(script, /DROP\s+DATABASE/i);
  assert.doesNotMatch(script, /WITH\s+REPLACE/i);
});
