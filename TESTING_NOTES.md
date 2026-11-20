# Testing Notes

## 1. Test results
I wrote 20 tests in total. All of them pass and none fail.

## 2. Line coverage
Overall line coverage is 90.5%, taken from coverage-report/Summary.txt. Domain is at 100% and Application is at 95.8%. Infrastructure shows 0% because I mocked the repository in my tests, so the real one never runs.

## 3. Where I used Moq, AutoFixture and Bogus
- **Moq:** I used it to mock the inventory service, the order repository and the notification service. It also let me check that Save and Send were called once on success and never on failure.
- **AutoFixture:** I used it in Test 3.1 to generate an order, with a customization so quantity is always above 0 and price is never negative.
- **Bogus:** I used it in Test 3.2 to generate a realistic customer name and email. I set a fixed seed so I get the same values on every run.

## 4. Is the discount fair?
Not really. A customer who pays 999.99 gets no discount, while a customer who buys for 1,000 gets 100 off and pays only 900, so spending more ends up costing less.

## 5. Bugs found
None.