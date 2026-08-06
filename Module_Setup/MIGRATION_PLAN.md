# Module_Setup Migration Plan

## 1. Purpose
Implement the workstation setup workflow as a shell-embedded, multi-step experience.

## 2. Current Delivery Slice
- Workflow state and model foundation.
- Work order validation and sample lookup orchestration.
- Step pages for work order, part, sequence, dunnage type, dunnage part, and review.
- Shell navigation entry and page-service registration.
- Receiving-parity dunnage UI for type selection, part selection, review tabs, and quick-add actions.
- Pair assignment actions (add/remove/remove-all-for-type/clear-all-for-pair) with save-on-review semantics.
- Role-gated quick add for dunnage type/part writes to mtm_receiving_application.

## 3. Follow-Up Slices
- Replace sample lookup and persistence stubs with the real Infor Visual and MySQL paths.
- Expand tests around the workflow service and page/view-model transitions.
- Add viewmodel command tests for role-gated quick add and tabbed review data shape.