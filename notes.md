# Accept Criteria

Som administrator
vil jeg kunne aktivere og deaktivere specifikke målepunkter via Admin-portalen
så jeg kan styre, hvilke målepunkter der udsteder certifikater.

**As** an admin user
**I want to** turn ON/OFF specific metering points from admin menu
**So that I** can support single MP's running on ETT while others run in G-REX

## It must be possible to select a company in the Admin portal and see a list of all its metering points.

## For each metering point, there must be a column called "Manage", where the button shows either "Activate" or "Disable", depending on the current status of the metering point.

## When a metering point is activated, it must start issuing certificates.

## When a metering point is deactivated, it must stop issuing certificates.

## It must **not** be possible for UI users to change the status of metering points via the "Metering Points" menu item on ett.dk – the edit option must be removed.

## It must not be possible for API users to activate/deactivate metering points.

## For API calls that attempt to change a metering point, the system must return an error message similar to: "Metering point controlled by admin - not possible to edit".

- Would recommend just returning 401 and not giving a reason why, to limit information disclosure and enhance security. Avoid detailed error messages that could reveal system internals to unauthorized users.

## A dialog box must be shown to the Admin when they click "Activate" or "Disable". The message should be (something with a warning.....)


# Issues that need to be solved:

- Add "Manage" function to: Admin Portal - Organizations - Metering Points, where metering points can be activated or disabled
  - New "Manage" column that shows "Activate" or "Disable"
  - A dialog box must be shown to the Admin when they click "Activate" or "Disable". The message should be (something with a warning.....)
  -

- Metering point activation and deactivation
  - Enabling metering point: Create issuing contract for the metering point
  - Disabling metering point: Delete issuing contract for the metering point

- Disable/remove the activation/deactivation of metering points in: ETT - Metering Points

- It should not be possible for API users to activate/disable metering points
  - Return 401 wihout giving too much information to the caller as to why they don't have access
