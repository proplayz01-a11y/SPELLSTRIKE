# SPELLSTRIKE - Deferred Issues

Only issues explicitly deferred by the user belong here. Verify a fix before closing an item.


## Unity AI service TLS certificate mismatch (2026-10-27)

- [ ] **TO BE FIXED LATER** at user request; continue Panels 07-10 first.
- Screenshot shows a warning while Unity Stage3Scene Game View is open: **Hostname mismatch in secure connection**.
- Requested server: `generators.ai.unity.com`; certificate hostname shown: `SN-131703000545`.
- Do not click **Yes** to bypass certificate verification. Use **No** to dismiss if still present.
- Later: review Editor.log, Unity AI service settings, computer clock, trusted certificates, DNS/proxy interception and official Unity package guidance.
- No Stage3 gameplay, UI, Android, material, renderer, or certificate settings were changed to fix it.

