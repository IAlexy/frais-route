# 🤝 Guide de Contribution — FraisRoute

Merci de votre intérêt pour **FraisRoute** ! Ce document décrit les prérequis, le workflow de développement, les règles de qualité de code (Sonar) et l'exécution des tests automatisés.

---

## 🛠️ Prérequis

- **.NET SDK** `8.0.x`
- **Node.js** `>= 22.0.0` (testé avec Node 24 LTS)
- **npm** `>= 10.0.0`

---

## 🚀 Installation Locale

```bash
# 1. Cloner le dépôt
git clone https://github.com/IAlexy/frais-route.git
cd frais-route

# 2. Restaurer la solution .NET (Backend + Tests)
dotnet restore backend/FraisRoute.sln

# 3. Installer les dépendances Frontend
cd frontend && npm install && cd ..
```

---

## 🧪 Exécution des Tests & Qualité du Code

Toute Pull Request doit respecter une politique **Zéro Warning / Zéro Erreur** sur le backend comme sur le frontend :

### Depuis la racine du projet

```bash
# Lancer l'ensemble des tests (Backend xUnit + Frontend Vitest)
npm test

# Vérifier l'analyse statique C# (SonarAnalyzer.CSharp) et le linter TS/Vue (Oxlint)
npm run lint

# Vérifier le typage TypeScript strict (vue-tsc)
npm run typecheck

# Compiler le backend (.NET Release) et le frontend (Vite production bundle)
npm run build
```

### Tests détaillés Backend (.NET 8 + xUnit)

```bash
cd backend

# Compilation stricte avec SonarAnalyzer.CSharp (0 warning requis)
dotnet build FraisRoute.sln -warnaserror

# Exécution des 64 tests (Calculateur fiscal Urssaf, Génération PDF QuestPDF, Intégration API SQLite)
dotnet test FraisRoute.sln
```

### Tests détaillés Frontend (Vue 3 + Vitest + JSDOM)

```bash
cd frontend

# Tests unitaires du client API
npm run test:unit

# Tests d'interface UI (Composants & Vues)
npm run test:ui

# Couverture de code complète (V8 / LCOV)
npm run test:coverage
```

---

## 📏 Standards d'Architecture & Qualité Sonar

1. **Backend (`backend/FraisRoute.Api`)** :
   - Architecture en **Minimal APIs** découpée par domaine dans `Endpoints/` (`VehicleEndpoints`, `ExpenseReportEndpoints`, `TripEndpoints`, `ReceiptEndpoints`, `CalculatorEndpoints`, `AddressEndpoints`).
   - L'analyseur `SonarAnalyzer.CSharp` est activé globalement via `backend/Directory.Build.props` avec `<AnalysisLevel>latest</AnalysisLevel>`.
   - Toute logique de barème fiscal doit être couverte unitairement dans `FrenchFiscalScaleCalculatorTests.cs`.
2. **Frontend (`frontend/src`)** :
   - Composants en **Vue 3 `<script setup lang="ts">`** avec typage strict (`noUnusedLocals`, `noUnusedParameters`).
   - Compatibilité **Web & Mobile (Capacitor)** : privilégier les APIs hybrides avec repli Web automatique (`@capacitor/camera`, `@capacitor/share`).
3. **Commits** :
   - Utilisez le format **Conventional Commits** (`feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`).
