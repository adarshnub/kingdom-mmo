const modules = [
  ['Players', 'Lookup, account status, sessions, and moderation history'],
  ['Live events', 'Schedule data-driven events and inspect phase transitions'],
  ['Compensation', 'Preview and approve audited mail grants'],
  ['Economy ledger', 'Trace every resource grant and spend by correlation ID'],
  ['Feature flags', 'Control staged rollout by environment and kingdom'],
  ['Audit trail', 'Review every privileged action and its stated reason'],
] as const;

async function getApiStatus(): Promise<'ready' | 'unavailable'> {
  try {
    const response = await fetch(
      `${process.env.API_BASE_URL ?? 'http://localhost:3000'}/health/ready`,
      {
        cache: 'no-store',
      },
    );
    return response.ok ? 'ready' : 'unavailable';
  } catch {
    return 'unavailable';
  }
}

export default async function Home() {
  const apiStatus = await getApiStatus();
  return (
    <main>
      <header>
        <div>
          <p className="eyebrow">KINGDOM 1 · OPERATIONS</p>
          <h1>Control the live kingdom responsibly.</h1>
          <p className="lede">
            This foundation is read-only until MFA-backed administrator identity and audited
            mutation APIs are configured.
          </p>
        </div>
        <div className={`status ${apiStatus}`}>
          <span aria-hidden="true" /> API {apiStatus}
        </div>
      </header>

      <section aria-labelledby="modules-heading">
        <h2 id="modules-heading">Operations modules</h2>
        <div className="grid">
          {modules.map(([name, description]) => (
            <article key={name}>
              <h3>{name}</h3>
              <p>{description}</p>
              <button type="button" disabled>
                Requires admin identity
              </button>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
