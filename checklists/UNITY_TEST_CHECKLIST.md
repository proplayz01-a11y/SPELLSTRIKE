# SPELLSTRIKE Unity Manual Test Checklist

## General
- [ ] No console errors.
- [ ] Player spawns correctly.
- [ ] Camera works.
- [ ] Player movement works.
- [ ] Word input still works.
- [ ] STRIKE/attack submission works.
- [ ] UI updates correctly.

## Enemy
- [ ] Enemy starts inactive/idle correctly.
- [ ] Node trigger activates enemy.
- [ ] `StartBattle()` is called.
- [ ] Enemy finds player.
- [ ] Enemy moves correctly.
- [ ] Enemy faces player.
- [ ] Enemy attacks only when in range.
- [ ] Enemy damage applies once per attack.
- [ ] Cooldown works.
- [ ] Enemy takes damage.
- [ ] Enemy dies once.
- [ ] Node completion works.

## Boss / Reward
- [ ] Boss HP displays.
- [ ] Death animation plays.
- [ ] Reward unlocks.
- [ ] Fragment unlocks.
- [ ] Manual pickup works.
- [ ] Auto-grant fallback works if designed.
- [ ] Stage clear UI appears if implemented.

## Debuff
- [ ] Normal tiles are selectable.
- [ ] Locked tiles are not selectable.
- [ ] Cracked tiles are selectable.
- [ ] Broken tiles are not selectable.
- [ ] Tile visual state updates.
- [ ] Debuff expires/restores properly if duration-based.
- [ ] Debuff does not break word submission.
