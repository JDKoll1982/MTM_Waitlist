-- Seed: seed_waitlist_requests_default
-- Engine: MySQL 5.7
-- Source: demo dataset for the Waitlist work delivered in PromptFiles 01-06:
--   01 lifecycle (Pending/Accepted/Completed/Canceled + requested/accepted/completed/canceled + note)
--   02 coil requests (Pickup Coil, Wrong Coil) and part/type variety for the coil card
--   03 list/detail across request types/subtypes (Coil, Pickup WIP/FG/NCM/Outside, Scrap, Flatstock, Forklift, Other)
--   04 requester cancel-own + "My Requests" (rows from the signed-in requester 6229 AND other requester 5000)
--   05/06 ignored-locations + location grid are NOT persisted in this MySQL table (mock/Infor Visual), so they
--       are exercised by the coil/request rows above plus app-side sample data.
-- Idempotent: TRUNCATE both tables first (this is the seed's own demo set), then insert deterministic rows.
-- Consumed by sp_waitlist_request_list / _get / _status_update / _audit_insert and the Waitlist UI.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE waitlist_requests_audit;
TRUNCATE TABLE waitlist_requests_queue;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_requests_queue (
    public_id, building, work_center, request_type, subtype, input_value, active_setup_job_id,
    work_center_name, requester_employee_number, requester_employee_name, status,
    requested_utc, target_time_utc, is_overdue, assigned_material_handler,
    cancellation_reason, canceled_utc, canceled_by_employee_number,
    note, accepted_utc, completed_utc, released_utc, created_utc, updated_utc
)
VALUES
    -- Pending (open) -> returned by sp_waitlist_request_list; drives list/card + wait time + status badge
    ('f0000000-0001-4000-8000-000000000001','Expo Drive','100-3','Coil','Pickup',NULL,'100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 35 MINUTE, (UTC_TIMESTAMP() - INTERVAL 35 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Coil pickup for press 100-3.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 35 MINUTE, UTC_TIMESTAMP() - INTERVAL 35 MINUTE),
    ('f0000000-0002-4000-8000-000000000002','Expo Drive','100-3','Coil','Wrong Coil @ press','Wrong material at press - expected MMC0001000.','100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 12 MINUTE, (UTC_TIMESTAMP() - INTERVAL 12 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Reports the staged coil is the wrong one.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 12 MINUTE, UTC_TIMESTAMP() - INTERVAL 12 MINUTE),
    ('f0000000-0003-4000-8000-000000000003','Expo Drive','100-6','Pickup','Pickup WIP',NULL,'100-6','100-6','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 50 MINUTE, (UTC_TIMESTAMP() - INTERVAL 50 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 50 MINUTE, UTC_TIMESTAMP() - INTERVAL 50 MINUTE),
    ('f0000000-0004-4000-8000-000000000004','Expo Drive','100-5','Pickup','Pickup FG',NULL,'100-5','100-5','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 22 MINUTE, (UTC_TIMESTAMP() - INTERVAL 22 MINUTE) + INTERVAL 45 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 22 MINUTE, UTC_TIMESTAMP() - INTERVAL 22 MINUTE),
    ('f0000000-0005-4000-8000-000000000005','Expo Drive','100-11','Pickup','Pickup NCM',NULL,'100-11','100-11','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 18 MINUTE, (UTC_TIMESTAMP() - INTERVAL 18 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 18 MINUTE, UTC_TIMESTAMP() - INTERVAL 18 MINUTE),
    ('f0000000-0006-4000-8000-000000000006','Expo Drive','100-12','Pickup','Outside Service',NULL,'100-12','100-12','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 9 MINUTE, (UTC_TIMESTAMP() - INTERVAL 9 MINUTE) + INTERVAL 60 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 9 MINUTE, UTC_TIMESTAMP() - INTERVAL 9 MINUTE),
    -- Other requester (5000) open requests -> excluded from signed-in user's "My Requests"; cancel-own by 6229 must be denied
    ('f0000000-0007-4000-8000-000000000007','Expo Drive','100-14','Pickup','Pickup WIP',NULL,'100-14','100-14','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 30 MINUTE, (UTC_TIMESTAMP() - INTERVAL 30 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 30 MINUTE, UTC_TIMESTAMP() - INTERVAL 30 MINUTE),
    ('f0000000-0008-4000-8000-000000000008','Expo Drive','100-15','Other','General Text Entry','Need a line supervisor for a quick walkthrough.','100-15','100-15','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 14 MINUTE, (UTC_TIMESTAMP() - INTERVAL 14 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 14 MINUTE, UTC_TIMESTAMP() - INTERVAL 14 MINUTE),
    ('f0000000-0009-4000-8000-000000000009','Vits Drive','V100-40','Forklift Assist',NULL,'Need a forklift to move a pallet to staging.','V100-40','V100-40','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 26 MINUTE, (UTC_TIMESTAMP() - INTERVAL 26 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 26 MINUTE, UTC_TIMESTAMP() - INTERVAL 26 MINUTE),
    -- Accepted (In Progress) -> returned by list; claimed by a handler; lifecycle accepted_utc
    ('f0000000-0010-4000-8000-000000000010','Expo Drive','100-7','Coil','Pickup',NULL,'100-7','100-7','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 80 MINUTE, (UTC_TIMESTAMP() - INTERVAL 80 MINUTE) + INTERVAL 30 MINUTE, 1, '6229', NULL, NULL, NULL,
        'Overdue coil move in progress.', UTC_TIMESTAMP() - INTERVAL 75 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 80 MINUTE, UTC_TIMESTAMP() - INTERVAL 75 MINUTE),
    ('f0000000-0011-4000-8000-000000000011','Expo Drive','100-8','Other','General Text Entry','Changeover review requested.','100-8','100-8','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 25 MINUTE, (UTC_TIMESTAMP() - INTERVAL 25 MINUTE) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 20 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 25 MINUTE, UTC_TIMESTAMP() - INTERVAL 20 MINUTE),
    ('f0000000-0012-4000-8000-000000000012','Expo Drive','100-16','Pickup','Pickup NCM',NULL,'100-16','100-16','5000','Other User','Accepted',
        UTC_TIMESTAMP() - INTERVAL 40 MINUTE, (UTC_TIMESTAMP() - INTERVAL 40 MINUTE) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 33 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 40 MINUTE, UTC_TIMESTAMP() - INTERVAL 33 MINUTE),
    -- Completed (Done) -> resolved, retained; accepted_utc + completed_utc
    ('f0000000-0013-4000-8000-000000000013','Expo Drive','100-9','Scrap','Empty','Scrapped punch slugs at press 100-9.','100-9','100-9','6229','John Koll','Completed',
        UTC_TIMESTAMP() - INTERVAL 3 HOUR, (UTC_TIMESTAMP() - INTERVAL 3 HOUR) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        'Lugger filled and confirmed.', UTC_TIMESTAMP() - INTERVAL 170 MINUTE, UTC_TIMESTAMP() - INTERVAL 120 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 3 HOUR, UTC_TIMESTAMP() - INTERVAL 120 MINUTE),
    ('f0000000-0014-4000-8000-000000000014','Vits Drive','V100-41','Pickup','Pickup FG',NULL,'V100-41','V100-41','5000','Other User','Completed',
        UTC_TIMESTAMP() - INTERVAL 4 HOUR, (UTC_TIMESTAMP() - INTERVAL 4 HOUR) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 230 MINUTE, UTC_TIMESTAMP() - INTERVAL 150 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 4 HOUR, UTC_TIMESTAMP() - INTERVAL 150 MINUTE),
    -- Canceled -> cancel-own path (file 04): creator cancels own Waiting request; canceled_by = creator
    ('f0000000-0015-4000-8000-000000000015','Expo Drive','100-19','Pickup','Outside Service',NULL,'100-19','100-19','6229','John Koll','Canceled',
        UTC_TIMESTAMP() - INTERVAL 2 HOUR, (UTC_TIMESTAMP() - INTERVAL 2 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Operator no longer needs this pickup.', UTC_TIMESTAMP() - INTERVAL 110 MINUTE, '6229',
        'Canceled by requester before handling.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 2 HOUR, UTC_TIMESTAMP() - INTERVAL 110 MINUTE),
    ('f0000000-0016-4000-8000-000000000016','Expo Drive','100-20','Coil','Pickup',NULL,'100-20','100-20','5000','Other User','Canceled',
        UTC_TIMESTAMP() - INTERVAL 5 HOUR, (UTC_TIMESTAMP() - INTERVAL 5 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Duplicate of an earlier request.', UTC_TIMESTAMP() - INTERVAL 280 MINUTE, '5000',
        'Other requester canceled own request.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 5 HOUR, UTC_TIMESTAMP() - INTERVAL 280 MINUTE);

-- Audit rows so the Waitlist audit trail (file 01/03) and future analytics have transitions to read.
INSERT INTO waitlist_requests_audit (
    public_id, request_public_id, from_status, to_status, event_type,
    actor_employee_number, actor_employee_name, details, occurred_utc
)
VALUES
    ('a0000000-0001-4000-8000-000000000001','f0000000-0001-4000-8000-000000000001',NULL,NULL,'Created','6229','John Koll','Request submitted for press 100-3.',UTC_TIMESTAMP() - INTERVAL 35 MINUTE),
    ('a0000000-0002-4000-8000-000000000002','f0000000-0002-4000-8000-000000000002',NULL,NULL,'Created','6229','John Koll','Wrong coil reported.',UTC_TIMESTAMP() - INTERVAL 12 MINUTE),
    ('a0000000-0003-4000-8000-000000000003','f0000000-0007-4000-8000-000000000007',NULL,NULL,'Created','5000','Other User','Other requester WIP request.',UTC_TIMESTAMP() - INTERVAL 30 MINUTE),
    ('a0000000-0010-4000-8000-000000000010','f0000000-0010-4000-8000-000000000010',NULL,'Pending','Created','6229','John Koll','Request created.',UTC_TIMESTAMP() - INTERVAL 80 MINUTE),
    ('a0000000-0011-4000-8000-000000000011','f0000000-0010-4000-8000-000000000010','Pending','Accepted','Accepted','6229','John Koll','Handler claimed overdue coil move.',UTC_TIMESTAMP() - INTERVAL 75 MINUTE),
    ('a0000000-0013-4000-8000-000000000013','f0000000-0013-4000-8000-000000000013',NULL,'Pending','Created','6229','John Koll','Scrap request created.',UTC_TIMESTAMP() - INTERVAL 3 HOUR),
    ('a0000000-0014-4000-8000-000000000014','f0000000-0013-4000-8000-000000000013','Pending','Accepted','Accepted','6229','John Koll','Scrap accepted.',UTC_TIMESTAMP() - INTERVAL 170 MINUTE),
    ('a0000000-0015-4000-8000-000000000015','f0000000-0013-4000-8000-000000000013','Accepted','Completed','Completed','6229','John Koll','Scrap completed.',UTC_TIMESTAMP() - INTERVAL 120 MINUTE),
    ('a0000000-0016-4000-8000-000000000016','f0000000-0015-4000-8000-000000000015',NULL,'Pending','Created','6229','John Koll','Outside-service request created.',UTC_TIMESTAMP() - INTERVAL 2 HOUR),
    ('a0000000-0017-4000-8000-000000000017','f0000000-0015-4000-8000-000000000015','Pending','Canceled','Canceled','6229','John Koll','Requester canceled own request.',UTC_TIMESTAMP() - INTERVAL 110 MINUTE),
    ('a0000000-0018-4000-8000-000000000018','f0000000-0016-4000-8000-000000000016',NULL,'Pending','Created','5000','Other User','Coil request created.',UTC_TIMESTAMP() - INTERVAL 5 HOUR),
    ('a0000000-0019-4000-8000-000000000019','f0000000-0016-4000-8000-000000000016','Pending','Canceled','Canceled','5000','Other User','Duplicate - canceled by requester.',UTC_TIMESTAMP() - INTERVAL 280 MINUTE);
