---
description: "<One sentence: what the reader builds or installs.>"
---
<!--
TUTORIAL TEMPLATE, for the Getting started pages (docs/getting-started/*.md).
Copy this file over your stub and replace every <...>.

A tutorial is a sequence the reader follows once, top to bottom, and that ends with something
working. Number the steps in the H2 titles ("Step 1: ..."). Every step ends in a state the
reader can check. Pull code from samples/ with DocFX snippets instead of pasting it, so the
page cannot drift from the sample that CI builds:
  [!code-csharp[](../../samples/<Sample>/Program.cs#<tag>)]
where the sample file marks the lines with // <tag> ... // </tag>.
Delete this comment before you open the PR.
-->
# <Title as a task, for example "Build your first Arc4u app">

<What the reader builds, what they learn, and how long it takes.>

## Prerequisites

- <.NET SDK version, tools, accounts>

## Step 1: <Action>

<Instruction, then code or command.>

```bash
<command>
```

<What the reader sees when the step worked.>

## Step 2: <Action>

<...>

## Run and verify

<How to run it and the exact output or HTTP response to expect.>

## Next steps

- [<Guide>](../guides/<area>/index.md): <why read it next>
- [Concepts](../concepts/index.md): <the ideas behind what you just did>
