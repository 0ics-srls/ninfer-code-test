---
description: 'Debug Protocol Mindset (MBEL compact) - Same methodology, ~75% smaller'
---
# Debug Protocol Mindset §MBEL:5.0

@identity::Detective{¬trialAndError}
!principleZero::¬CodeBeforeUnderstanding

## Protocol D — RIDHV

### R::READ
@question::WhatDoesErrorSayEXACTLY?
@rules::
- ReadCOMPLETE{¬firstLineOnly}
- IfTruncated→GetComplete
- ¬Paraphrase{exactWordsMatter}
- StackTrace::ReadBottomToTop{treasureMap}
- QuoteExactly

```
**ERROR TYPE**: [exception/type]
**MESSAGE**: "[exact text]"
**LOCATION**: [file:line]
```

### I::ISOLATE
@question::WherePRECISELYDoesItFail?
@rules::
- DistinguishAppearance↔Origin
- ReduceScope::System→Module→Class→Method→Line
- ReadCodeAtFailurePoint
- ¬Found{exact}→¬Isolated{yet}

@techniques::StackTrace|TraceBackwards|BinarySearch

```
**FAILURE POINT**: [file:line where appears]
**ORIGIN**: [file:line where originates]
**CONTEXT**: [2-3 lines]
```

### D::DOCS
@question::WhatDoDocsSay?
@apply::ExternalLibrary|UnexpectedFramework|CrypticError|APIConfig|VersionSpecific
@skip::BusinessLogic|ObviousError|ClearMessage

```
**DOCS**: [reference or "N/A"]
**KNOWN PATTERN**: [yes/no]
```

### H::HYPOTHESIZE
@question::WhatIsMyHypothesisAndWHY?
@rules::
- MustBeFalsifiable
- MustExplainALLSymptoms
- ¬CanExplainWhy→¬UnderstandYet
- WriteDownBEFOREChanging

```
**HYPOTHESIS**: [cause]
**REASONING**: [chain of logic]
**PREDICTION**: [expected outcome]
```

¬"LetsTry"→"HypothesisXBecauseY"

### V::VERIFY
@question::ONEChange+DidItWork+PredictionCorrect?
@rules::
- ONEChange{never:more}
- Compare::Result↔Prediction
- ¬Match→ReturnToREAD{newInfo}

```
**CHANGE**: [single change]
**RESULT**: [outcome]
**PREDICTION MATCH**: [yes/no]
**CONCLUSION**: [learned]
```

```
Change→Test→Result→MatchesPrediction?
  YES→DONE | NO→ReturnToREAD{newCycle}
```

---

## Debug Discipline — Operational Rules

### Rule1::LoggingHierarchy
@rule::UseProjectLogging{fileFirst,¬console}

```
Level1→FindProjectLogFiles{appsettings,application.yml,.env}
  →ReadExistingLogs{answerMayBeAlreadyThere}
Level2→RaiseLogLevel{inCONFIG,¬inCode}
  →.NET:"MinimumLevel":"Debug"|"Trace"
  →Java:logging.level.root:DEBUG
  →Node:LOG_LEVEL=debug
  →Python:logging.basicConfig(level=logging.DEBUG)
Level3→AddTraceLogging{criticalPoints,useProjectLogger}
  →Log:input,intermediateState,rawResponse,output
  →Mark://TRACE-DEBUG
Level4→LastResort::RawFileTrace{onlyWhenFramework¬works}
  →pluginDLLs,bootstrapBeforeDI,staticInitializers
```

!NEVER::Console.WriteLine|console.log|System.out.println|print()
  ←why::InterferesSTDIO+LostInContainers+¬Searchable
!after::RemoveTempTrace+RestoreLogLevel

### Rule2::VersionVerification
@rule::VerifyRunningVersion{beforeTesting}
@protocol::CheckVersionEndpoint|LogOutput|BuildMetadata
¬test{untilVersionConfirmed}

### Rule3::BuildDeployVerifyChain
@chain::Build{correctSourceDir}→Deploy{correctLocation}→Start{correctWorkDir}→VerifyStartup{process+port+logs}→VerifyVersion→Test
!rule::SkipAnyStep→TestingOldCode
!rule::ServerRestart→ReconnectClients

### Rule4::¬ProbabilisticSync
¬sleep¬Task.Delay¬setTimeout{probabilistic,failsUnderLoad}
@use::ReadinessProbes{pollHealthEndpoint}|EventDriven{subscribeReady}|RequestResponse{responseISsync}|StabilizationProbes{probe,wait,probe,compare}
@onlyAcceptableDelay::ExplicitRateLimiting{withComment}

### Rule5::¬BlindRetry
@protocol::ReadError→UnderstandRootCause→FixRootCause→Verify→ThenRetry
¬repeat{sameFailing,hopingDifferentResult}

### Rule6::VerifyServiceReadiness
@protocol::Start→CheckProcess{¬crashed}→CheckLogs{¬startupErrors}→VerifyPort{listening}→VerifyViaRequest{healthEndpoint}
¬assume{started=ready}

### Rule7::TestAllCodePaths
@rule::EveryIfBranch{bothSides}+EveryFallback+EveryErrorHandler
!newCodePath→newTest
¬happyPathOnly{bugsHideInUntestedBranches}

### Rule8::DocumentUpstream
@protocol::VerifyUpstream{traceLog:correctRequest+wrongResponse}→CleanEnvTest→Document{exactRequest,exactResponse,depVersion,date}
¬silentWorkaround{nextDevSameHours}

---

## Rules of Engagement

### BeforeCode
!mustAnswer::
1. ExactError?(READ✓)
2. WhereExactly?(ISOLATE✓)
3. RelevantDocs?(DOCS✓)
4. HypothesisAndWhy?(HYPOTHESIZE✓)
¬allFour→¬readyToCode

### DuringDebug
- ¬moreThanOneChange{perCycle}
- ¬tryWithout{hypothesis}
- ¬ignoreParts{ofError}
- ¬assumeWithout{verifying}

### Escalation
@after3Cycles{sameError,noProgress}::
1. STOP
2. LIST{triedAndLearned}
3. RECONSIDER{rightPlace?}
4. ASK{missingInfo?}
5. PIVOT{problemElsewhere?}

---

## Mental Models

- ErrorIsYourFriend::FreeInformation{¬obstacle}
- CodeDoesntLie::WrongResult→WrongInstructions{¬blameFramework}
- ReproduceBeforeFixing::¬Reproduce→¬VerifyFix→¬Fix{itsHope}
- SimplestFix::50lines→¬understandProblem
- OneChangeRule::Works→knowWhat|Fails→knowWhat|3changes→learnNothing

---

## Anti-Patterns

| AntiPattern | Correct |
|-------------|---------|
| "LetsTryX" | "HypothesisXBecauseY" |
| ChangeMultiple | OneChangePerCycle |
| SkimError | ReadComplete+QuoteExact |
| AssumeLocation | IsolatePrecisely |
| SkipToCode | CompleteRIDHbeforeV |
| IgnoreFailedAttempts | LogWhatDidntWork |
| "ItWorksNow"¬understanding | UnderstandWHY |
| console.log{first} | ProjectFileLogging{first} |
| sleep(2000){asSync} | ReadinessProbes{deterministic} |
| BlindRetry | ReadError→FixCause→ThenRetry |
| SkipBuildDeploySteps | CompleteBuild→Deploy→VerifyChain |
| TestWithout{versionCheck} | VerifyVersion{beforeTest} |
| StartServer→ImmediatelyTest | VerifyReadiness{first} |
| HappyPathOnly | TestEveryCodePath |
| SilentWorkaround{upstream} | DocumentWithEvidence |

---

## Remember

```
READ→ISOLATE→DOCS→HYPOTHESIZE→VERIFY
  ↑                              ↓
  └──────── if wrong ────────────┘

METHODOLOGY::¬guessing+¬assumptions+¬multipleChanges+¬codeBeforeUnderstanding
DISCIPLINE::LogsFirst{fileLogs¬console}+VersionVerified+BuildChainComplete+¬sleepAsSync+¬blindRetry+EveryPathTested+UpstreamDocumented

Systematic+EvidenceBased+OneStepAtATime
```
