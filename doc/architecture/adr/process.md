# Process for ADRs

We wish to document our decisions using ADRs and we want the process of accepting new ADRs to be streamlined. Furthermore, we do not want Pull Requests to be blocked for days or weeks due to a missing acceptance of an ADR.

The following describes our process for proposing new ADRs:

1. In the Pull Request where a change of architectural importance *) is added write an ADR with
    - Status = "Proposed"
    - Follow this naming convention for the .md file: `{running number}-PROPOSAL-{problem domain}.md` (use kebab-casing)
2. The Pull Request (including) the proposed ADR can be reviewed, and if approved merged into main.
3. At the upcoming "Developer Talk" the proposed ADR will be discussed and it will be accepted or rejected by the team.

*) Adding a new nuget dependency is also a change of architectural importance.
