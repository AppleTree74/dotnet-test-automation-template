import { defineConfig } from "allure";

// Allure Report 3 configuration.
// - Durable history is a single JSONL file (guide section 14). CI restores it from a durable
//   store before generating and persists it afterwards.
// - The awesome plugin renders the human HTML report published to GitHub Pages.
// For AI-assisted debugging, Allure Agent Mode is available via the `allure agent` CLI
// (see docs/debugging.md); it is interactive and not part of report generation.
export default defineConfig({
  name: "Automation Template",
  output: "allure-report",
  historyPath: "allure-history/history.jsonl",
  appendHistory: true,
  plugins: {
    awesome: {
      import: "@allurereport/plugin-awesome",
      options: {
        reportName: "Automation Template",
      },
    },
  },
});
