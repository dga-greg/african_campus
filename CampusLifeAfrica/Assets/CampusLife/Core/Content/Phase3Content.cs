using System.Collections.Generic;
using CampusLife.Core.Academics;

namespace CampusLife.Core.Content
{
    /// <summary>
    /// Year 1, Semester 1 curriculum of Akwaaba Metropolitan University: two
    /// university-wide required courses, two electives, and two core courses
    /// for each of the ten programmes. Every course has four weekly topics with
    /// two key points each; each key point carries one multiple-choice question.
    /// </summary>
    public static class Phase3Content
    {
        static CourseCatalog _catalog;

        public static CourseCatalog Catalog => _catalog ?? (_catalog = Build());

        sealed class K
        {
            public string Text, Question;
            public string[] Options;
            public int Answer;
        }

        static K P(string text, string question, string options, int answer)
            => new K { Text = text, Question = question, Options = options.Split('|'), Answer = answer };

        sealed class T
        {
            public string Title;
            public K[] Points;
        }

        static T Topic(string title, K a, K b) => new T { Title = title, Points = new[] { a, b } };

        static CourseDefinition Course(string code, string title, string program, CourseKind kind, string lecturer, params T[] topics)
        {
            var built = new Topic[topics.Length];
            for (int i = 0; i < topics.Length; i++)
            {
                var kps = new KeyPoint[topics[i].Points.Length];
                for (int j = 0; j < kps.Length; j++)
                {
                    var k = topics[i].Points[j];
                    kps[j] = new KeyPoint($"{code.Replace(" ", "")}-t{i + 1}-k{j + 1}", k.Text, k.Question, k.Options, k.Answer);
                }
                built[i] = new Topic(i, topics[i].Title, kps);
            }
            return new CourseDefinition(code, title, 3, program, kind, lecturer, 100, 1, built);
        }

        static CourseCatalog Build()
        {
            var c = new CourseCatalog();
            foreach (var course in Courses()) c.Add(course);
            return c;
        }

        static IEnumerable<CourseDefinition> Courses()
        {
            // ---------------------------------------------------- university-wide
            yield return Course("AMU 101", "Academic Writing I", null, CourseKind.Core, "Dr. Yaa Oforiwaa Bediako",
                Topic("The writing process",
                    P("Good academic writing follows a process: planning, drafting, revising and editing.", "Which stage of the writing process comes first?", "Drafting|Editing|Planning|Revising", 2),
                    P("Revising improves ideas and structure; editing fixes grammar, spelling and punctuation.", "Correcting spelling and punctuation is part of…", "Planning|Editing|Brainstorming|Outlining", 1)),
                Topic("Paragraphs and thesis statements",
                    P("A thesis statement states an essay's main argument, usually at the end of the introduction.", "Where does a thesis statement usually appear?", "In the conclusion|At the end of the introduction|In the reference list|Only in the title", 1),
                    P("A good paragraph has a topic sentence, supporting evidence and a concluding or linking sentence.", "What does a topic sentence do?", "Lists the references|States the paragraph's main idea|Ends the essay|Quotes an expert", 1)),
                Topic("Using sources",
                    P("Plagiarism is presenting someone else's words or ideas as your own without acknowledgement.", "Copying a paragraph from a website without citing it is…", "Paraphrasing|Plagiarism|Summarising|Referencing", 1),
                    P("Paraphrasing restates a source's idea in your own words and still needs a citation.", "Does a paraphrase need a citation?", "No, it is in your own words|Only if it is long|Yes, always|Only in exams", 2)),
                Topic("Referencing",
                    P("APA style uses author–date in-text citations, for example (Mensah, 2021).", "Which in-text citation follows APA style?", "[1]|(Mensah, 2021)|Mensah, Kofi.|ibid.", 1),
                    P("A reference list at the end gives full details of every source cited in the text.", "What belongs in a reference list?", "Books you enjoyed|Every source cited in the text|Your own opinions|The essay outline", 1)));

            yield return Course("AMU 103", "Introduction to African Studies", null, CourseKind.Core, "Dr. Kwesi Ampong",
                Topic("Ghana's independence",
                    P("Ghana became independent from British colonial rule on 6 March 1957.", "On what date did Ghana gain independence?", "1 July 1960|6 March 1957|24 February 1966|7 January 1993", 1),
                    P("Before independence, the territory was known as the Gold Coast.", "What was Ghana called before independence?", "Gold Coast|Ivory Coast|Slave Coast|Grain Coast", 0)),
                Topic("Peoples and languages",
                    P("Ghana is home to many ethnic groups, including the Akan, Ewe, Ga-Adangbe and Mole-Dagbani.", "Which of these is one of Ghana's major ethnic groups?", "Zulu|Akan|Maasai|Amhara", 1),
                    P("English is Ghana's official language; Ghanaian languages such as Twi, Ewe, Ga and Dagbani are widely spoken.", "What is Ghana's official language?", "French|Twi|English|Hausa", 2)),
                Topic("Cloth and symbols",
                    P("Kente is a hand-woven cloth associated with Asante and Ewe weaving traditions.", "Kente cloth is…", "Machine-printed only|Hand-woven|Made from bark|Imported from Europe", 1),
                    P("Adinkra symbols represent proverbs and ideas, and are often stamped on cloth.", "What do Adinkra symbols represent?", "Proverbs and ideas|Football clubs|Currency values|Map coordinates", 0)),
                Topic("Geography of Ghana",
                    P("Lake Volta, created by the Akosombo Dam, is one of the world's largest artificial lakes by surface area.", "What created Lake Volta?", "A meteor|The Akosombo Dam|Melting glaciers|An earthquake", 1),
                    P("Ghana is divided into 16 administrative regions.", "How many administrative regions does Ghana have?", "10|12|16|20", 2)));

            yield return Course("AMU 105", "Critical Thinking", null, CourseKind.Elective, "Dr. Selasi Agbenyega",
                Topic("Arguments",
                    P("An argument is a set of reasons (premises) offered to support a conclusion.", "In an argument, the reasons given are called…", "Conclusions|Premises|Fallacies|Hypotheses", 1),
                    P("The conclusion is the claim the premises are meant to support.", "The claim an argument tries to establish is the…", "Premise|Conclusion|Assumption|Example", 1)),
                Topic("Deduction and induction",
                    P("In a valid deductive argument, if the premises are true the conclusion must be true.", "In a valid deductive argument with true premises, the conclusion…", "Is probably true|Must be true|Is irrelevant|Must be false", 1),
                    P("Inductive reasoning draws probable conclusions from patterns or evidence.", "Concluding 'the sun will rise tomorrow' from past experience is…", "Deductive reasoning|Inductive reasoning|Circular reasoning|An appeal to authority", 1)),
                Topic("Fallacies",
                    P("An ad hominem fallacy attacks the person instead of their argument.", "'Don't trust her plan — she failed maths in JHS' is…", "A straw man|An ad hominem|A slippery slope|A false dilemma", 1),
                    P("A false dilemma presents only two options when more exist.", "'Either you support me or you hate this school' is…", "A false dilemma|A red herring|A valid argument|An analogy", 0)),
                Topic("Evaluating evidence",
                    P("Credible sources are reliable, current and written by people with relevant expertise.", "Which is usually the most credible source for a medical claim?", "An anonymous WhatsApp message|A peer-reviewed medical journal|A celebrity's post|A comment section", 1),
                    P("Correlation does not prove causation.", "If two things happen together, this alone proves…", "One causes the other|Nothing about causation|They are the same thing|Both are false", 1)));

            yield return Course("AMU 107", "Introduction to French", null, CourseKind.Elective, "Madame Edem Kpodo",
                Topic("Greetings",
                    P("'Bonjour' means 'hello/good morning' and 'Bonsoir' means 'good evening'.", "How do you say 'good evening' in French?", "Bonjour|Bonsoir|Merci|Au revoir", 1),
                    P("'Merci' means 'thank you' and 'Au revoir' means 'goodbye'.", "'Au revoir' means…", "Thank you|Please|Goodbye|Good morning", 2)),
                Topic("Introducing yourself",
                    P("'Je m'appelle Kofi' means 'My name is Kofi'.", "'Je m'appelle Ama' means…", "I am from Ama|My name is Ama|I like Ama|Ama is here", 1),
                    P("'Je suis étudiant' (or 'étudiante') means 'I am a student'.", "'Je suis étudiante' means…", "I am a teacher|I am a student|I am tired|I am Ghanaian", 1)),
                Topic("Numbers",
                    P("Un, deux, trois, quatre, cinq are the numbers 1 to 5.", "What number is 'trois'?", "2|3|4|5", 1),
                    P("'Dix' is 10 and 'vingt' is 20.", "What number is 'vingt'?", "10|12|20|100", 2)),
                Topic("French around Ghana",
                    P("Ghana's neighbours Côte d'Ivoire, Togo and Burkina Faso use French as an official language.", "Which neighbour of Ghana uses French as an official language?", "Nigeria|Togo|Liberia|Sierra Leone", 1),
                    P("'Où est la bibliothèque ?' means 'Where is the library?'", "'La bibliothèque' is…", "The cafeteria|The library|The hostel|The bus", 1)));

            // ------------------------------------------------- Computer Science
            yield return Course("CSC 101", "Introduction to Computing", "computer_science", CourseKind.Core, "Dr. Akua Frimpong",
                Topic("Hardware and software",
                    P("The CPU (central processing unit) executes program instructions.", "Which component executes program instructions?", "Monitor|CPU|Keyboard|Printer", 1),
                    P("RAM is temporary working memory that is cleared when the computer is switched off.", "What happens to RAM contents when the power goes off?", "Saved permanently|Lost|Printed|Uploaded", 1)),
                Topic("Binary",
                    P("Computers represent data in binary, using only the digits 0 and 1.", "Binary uses which digits?", "0–9|0 and 1|1 and 2|A–F", 1),
                    P("The binary number 101 equals 5 in decimal.", "What is binary 101 in decimal?", "3|5|6|101", 1)),
                Topic("Units of data",
                    P("A byte is 8 bits.", "How many bits are in a byte?", "4|8|16|1024", 1),
                    P("A kilobyte is about a thousand bytes; a megabyte is about a million bytes.", "Which is the largest?", "1 kilobyte|1 megabyte|1 byte|1 bit", 1)),
                Topic("Networks and operating systems",
                    P("The Internet is a global network of networks that communicate using shared protocols.", "The Internet is best described as…", "A single computer|A global network of networks|A web browser|An email app", 1),
                    P("An operating system manages hardware and runs applications; examples are Windows, Linux and Android.", "Which of these is an operating system?", "Android|Google Search|Excel|WhatsApp", 0)));

            yield return Course("CSC 103", "Programming Fundamentals", "computer_science", CourseKind.Core, "Mr. Elikem Dzokoto",
                Topic("Algorithms",
                    P("An algorithm is a finite, step-by-step procedure for solving a problem.", "An algorithm is…", "A computer brand|A step-by-step procedure to solve a problem|A type of virus|A programming language", 1),
                    P("Pseudocode describes an algorithm in structured plain language before coding.", "Writing an algorithm in structured plain language is called…", "Pseudocode|Machine code|Debugging|Compiling", 0)),
                Topic("Variables and data types",
                    P("A variable is a named storage location whose value can change.", "A variable is…", "A fixed number|A named storage location for a value|A loop|An error", 1),
                    P("Common data types include integers, floats, strings and booleans (true/false).", "A value that is either true or false is a…", "String|Integer|Boolean|Float", 2)),
                Topic("Control flow",
                    P("An if statement runs code only when a condition is true.", "Which structure makes a decision?", "A loop|An if statement|A variable|A comment", 1),
                    P("A loop repeats a block of code, as with 'for' and 'while' loops.", "To print the numbers 1 to 100 you would most likely use…", "A loop|A comment|A constant|A boolean", 0)),
                Topic("Debugging",
                    P("A syntax error breaks the language's rules and stops the program from running.", "Forgetting a closing bracket causes a…", "Logic error|Syntax error|Network error|Hardware fault", 1),
                    P("A logic error lets a program run but produces the wrong result.", "A program runs but calculates the wrong average. This is a…", "Syntax error|Logic error|Hardware fault|Compiler bug", 1)));

            // ------------------------------------------- Business Administration
            yield return Course("BUS 101", "Principles of Management", "business_administration", CourseKind.Core, "Prof. Emmanuel Tagoe",
                Topic("Functions of management",
                    P("The four functions of management are planning, organising, leading and controlling.", "Which is one of the four functions of management?", "Advertising|Controlling|Banking|Auditing", 1),
                    P("Planning means setting goals and deciding how to achieve them.", "Setting goals and deciding how to reach them is…", "Planning|Controlling|Leading|Staffing", 0)),
                Topic("Organising",
                    P("An organisational chart shows roles and reporting relationships.", "An organisational chart shows…", "Profits|Roles and reporting lines|Market prices|Tax rates", 1),
                    P("Span of control is the number of people who report directly to a manager.", "The number of direct reports a manager has is the…", "Chain of command|Span of control|Mission|Budget", 1)),
                Topic("Leading and motivating",
                    P("Maslow's hierarchy places physiological needs at the base and self-actualisation at the top.", "In Maslow's hierarchy, which need is at the top?", "Safety|Esteem|Self-actualisation|Physiological", 2),
                    P("Leadership is the ability to influence people toward goals.", "Influencing people toward goals is…", "Leadership|Accounting|Forecasting|Auditing", 0)),
                Topic("SWOT analysis",
                    P("SWOT analysis examines Strengths, Weaknesses, Opportunities and Threats.", "In SWOT, the 'O' stands for…", "Operations|Opportunities|Objectives|Outputs", 1),
                    P("Strengths and weaknesses are internal; opportunities and threats are external.", "Which are external factors in SWOT?", "Strengths and weaknesses|Opportunities and threats|Strengths and threats|Weaknesses and opportunities", 1)));

            yield return Course("BUS 103", "Introduction to Marketing", "business_administration", CourseKind.Core, "Mrs. Adjoa Owusu-Ansah",
                Topic("The marketing mix",
                    P("The marketing mix (the 4 Ps) is Product, Price, Place and Promotion.", "Which is NOT one of the 4 Ps?", "Price|Place|Profit|Promotion", 2),
                    P("Place is about how and where the product reaches customers (distribution).", "Distribution channels belong to which P?", "Product|Place|Price|Promotion", 1)),
                Topic("Segmentation and targeting",
                    P("Market segmentation divides a market into groups of customers with similar needs.", "Market segmentation means…", "Dividing a market into groups with similar needs|Lowering prices|Building factories|Hiring staff", 0),
                    P("A target market is the segment a business chooses to serve.", "The customer group a business chooses to serve is its…", "Target market|Supply chain|Board|Competitor", 0)),
                Topic("Consumer behaviour",
                    P("Buyers often move through need recognition, information search, evaluation, purchase and post-purchase behaviour.", "Which stage usually comes first?", "Purchase|Need recognition|Evaluation|Post-purchase behaviour", 1),
                    P("Word of mouth — customers sharing opinions — strongly influences buying.", "Students recommending Auntie Akosua's meat pies to friends is…", "Word of mouth|Price skimming|Wholesaling|Branding law", 0)),
                Topic("Brands and promotion",
                    P("A brand is the name, design and identity that distinguish a product from competitors.", "The name and identity that set a product apart is its…", "Brand|Invoice|Ledger|Tariff", 0),
                    P("Promotion includes advertising, sales promotion, personal selling and public relations.", "Which is a form of promotion?", "Advertising|Warehousing|Manufacturing|Auditing", 0)));

            // ------------------------------------------------------- Accounting
            yield return Course("ACC 101", "Financial Accounting I", "accounting", CourseKind.Core, "Mr. Nana Kwame Boadu",
                Topic("The accounting equation",
                    P("The accounting equation is Assets = Liabilities + Equity.", "Which is the accounting equation?", "Assets = Liabilities − Equity|Assets = Liabilities + Equity|Equity = Assets + Liabilities|Liabilities = Assets + Equity", 1),
                    P("If assets are GH₵10,000 and liabilities GH₵4,000, equity is GH₵6,000.", "Assets GH₵10,000, liabilities GH₵4,000. What is equity?", "GH₵14,000|GH₵6,000|GH₵4,000|GH₵10,000", 1)),
                Topic("Double entry",
                    P("Double-entry bookkeeping records each transaction as a debit in one account and a credit in another.", "Double entry records each transaction…", "In one account only|As a debit in one account and a credit in another|Only when cash moves|Only at year end", 1),
                    P("Total debits must always equal total credits.", "In double entry, total debits should…", "Exceed credits|Equal credits|Be zero|Be half of credits", 1)),
                Topic("Financial statements",
                    P("The income statement shows revenue, expenses and profit for a period.", "Which statement shows profit for a period?", "Statement of financial position|Income statement|Bank statement|Cash book", 1),
                    P("The statement of financial position shows assets, liabilities and equity at a point in time.", "Assets, liabilities and equity at a date are shown in the…", "Income statement|Statement of financial position|Budget|Invoice", 1)),
                Topic("Accounting concepts",
                    P("Under the accruals concept, income and expenses are recorded when earned or incurred, not when cash moves.", "Under accruals, income is recorded…", "When cash is received|When it is earned|Only at year end|Never", 1),
                    P("Depreciation spreads the cost of a non-current asset over its useful life.", "Spreading a laptop's cost over its useful life is…", "Depreciation|Inflation|Revenue|A dividend", 0)));

            yield return Course("ACC 103", "Business Mathematics", "accounting", CourseKind.Core, "Mrs. Gifty Anane",
                Topic("Percentages",
                    P("To find 15% of GH₵200, multiply 200 by 0.15 to get GH₵30.", "What is 15% of GH₵200?", "GH₵15|GH₵30|GH₵20|GH₵300", 1),
                    P("A price rising from GH₵50 to GH₵60 is a 20% increase.", "GH₵50 to GH₵60 is what percentage increase?", "10%|20%|25%|60%", 1)),
                Topic("Simple interest",
                    P("Simple interest = Principal × Rate × Time.", "Which is the simple interest formula?", "P + R + T|P × R × T|P ÷ R × T|(P × R) ÷ T", 1),
                    P("GH₵1,000 at 10% simple interest for 2 years earns GH₵200 interest.", "Simple interest on GH₵1,000 at 10% for 2 years?", "GH₵100|GH₵200|GH₵210|GH₵1,200", 1)),
                Topic("Compound interest",
                    P("Compound interest earns interest on previously earned interest.", "Interest earned on interest is…", "Simple interest|Compound interest|A discount|A commission", 1),
                    P("GH₵1,000 at 10% compounded yearly for 2 years grows to GH₵1,210.", "GH₵1,000 at 10% compounded yearly for 2 years grows to…", "GH₵1,200|GH₵1,210|GH₵1,100|GH₵1,020", 1)),
                Topic("Break-even analysis",
                    P("Break-even is the point where total revenue equals total costs.", "At break-even…", "Revenue equals costs|Profit is highest|Costs are zero|Sales double", 0),
                    P("Break-even units = Fixed costs ÷ (Price − Variable cost per unit).", "Fixed costs GH₵600, price GH₵10, variable cost GH₵4. Break-even units?", "60|100|150|600", 1)));

            // -------------------------------------------------------- Economics
            yield return Course("ECO 101", "Principles of Microeconomics", "economics", CourseKind.Core, "Dr. Fuseini Alhassan",
                Topic("Scarcity and choice",
                    P("Scarcity means wants are unlimited but resources are limited, so choices must be made.", "Scarcity means…", "Unlimited resources|Limited resources and unlimited wants|Low prices|High taxes", 1),
                    P("Opportunity cost is the value of the next best alternative given up.", "If you study instead of working a paid shift, the lost wages are your…", "Sunk cost|Opportunity cost|Fixed cost|Profit", 1)),
                Topic("Demand",
                    P("The law of demand: as price rises, quantity demanded falls, other things equal.", "If the price of sachet water rises, quantity demanded usually…", "Rises|Falls|Never changes|Doubles", 1),
                    P("A change in income or tastes shifts the whole demand curve.", "What shifts the demand curve (not just a movement along it)?", "A change in the good's own price|A change in consumer income|A change in quantity supplied|Nothing can", 1)),
                Topic("Supply and equilibrium",
                    P("The law of supply: as price rises, quantity supplied rises.", "As price rises, quantity supplied usually…", "Falls|Rises|Disappears|Is unaffected", 1),
                    P("The equilibrium price is where quantity demanded equals quantity supplied.", "Equilibrium is where…", "Demand equals supply|Price is zero|Supply exceeds demand|Government sets the price", 0)),
                Topic("Elasticity",
                    P("Price elasticity of demand measures how responsive quantity demanded is to a price change.", "Price elasticity of demand measures…", "Consumer taste|Responsiveness of quantity demanded to price|Total revenue|Production cost", 1),
                    P("Necessities like salt tend to have inelastic demand.", "Demand for a necessity like salt is usually…", "Perfectly elastic|Inelastic|Highly elastic|Zero", 1)));

            yield return Course("ECO 103", "Introduction to Statistics", "economics", CourseKind.Core, "Mrs. Dzifa Ahiable",
                Topic("Types of data",
                    P("Qualitative data describes categories; quantitative data is numerical.", "Students' programmes of study are which type of data?", "Quantitative|Qualitative|Continuous|Ratio", 1),
                    P("A sample is a subset of a population studied to learn about the whole.", "A subset of a population studied to learn about it is a…", "Sample|Census|Parameter|Outlier", 0)),
                Topic("Averages",
                    P("The mean is the sum of the values divided by the number of values.", "What is the mean of 2, 4 and 9?", "4|5|9|15", 1),
                    P("The median is the middle value when the data are in order.", "What is the median of 3, 7, 1, 9, 5?", "5|7|3|25", 0)),
                Topic("Spread",
                    P("The range is the highest value minus the lowest value.", "What is the range of 4, 10, 7, 2?", "6|8|10|2", 1),
                    P("Standard deviation measures how spread out values are around the mean.", "Standard deviation measures…", "The central value|Spread around the mean|The most common value|The sample size", 1)),
                Topic("Probability",
                    P("Probability ranges from 0 (impossible) to 1 (certain).", "A probability of 1 means the event is…", "Impossible|Unlikely|Certain|An even chance", 2),
                    P("The probability of rolling a 3 on a fair six-sided die is 1/6.", "Probability of rolling a 3 on a fair die?", "1/3|1/6|1/2|3/6", 1)));

            // ------------------------------------------------------ Engineering
            yield return Course("ENG 101", "Engineering Mathematics I", "engineering", CourseKind.Core, "Ing. Kofi Asiedu",
                Topic("Algebra",
                    P("To solve 2x + 6 = 14, subtract 6 and then divide by 2: x = 4.", "Solve 2x + 6 = 14.", "3|4|10|7", 1),
                    P("In the quadratic formula x = (−b ± √(b² − 4ac)) / 2a, the expression b² − 4ac is the discriminant.", "In the quadratic formula, b² − 4ac is called the…", "Gradient|Discriminant|Intercept|Coefficient", 1)),
                Topic("Straight-line graphs",
                    P("In y = mx + c, m is the gradient and c is the y-intercept.", "In y = 3x + 2, the gradient is…", "2|3|5|x", 1),
                    P("A straight line through (0, 0) and (2, 6) has a gradient of 3.", "Gradient of the line through (0, 0) and (2, 6)?", "2|3|6|12", 1)),
                Topic("Trigonometry",
                    P("In a right-angled triangle, sin θ = opposite ÷ hypotenuse.", "sin θ equals…", "Adjacent ÷ hypotenuse|Opposite ÷ hypotenuse|Opposite ÷ adjacent|Hypotenuse ÷ opposite", 1),
                    P("sin 30° = 0.5.", "sin 30° =", "0|0.5|1|√3", 1)),
                Topic("Introduction to calculus",
                    P("A derivative measures the rate of change of a function.", "A derivative measures…", "The area under a curve|The rate of change|The average value|The intercept", 1),
                    P("The derivative of x² is 2x.", "d/dx (x²) =", "x|2x|x³/3|2", 1)));

            yield return Course("ENG 103", "Introduction to Engineering Design", "engineering", CourseKind.Core, "Ing. Mrs. Naa Dedei Quaye",
                Topic("The design process",
                    P("The engineering design process: define the problem, research, generate ideas, prototype, test and improve.", "Which step usually comes first?", "Testing|Defining the problem|Prototyping|Mass production", 1),
                    P("A prototype is an early model built to test a design.", "A prototype is…", "The final product|An early model for testing|A patent|A stamped blueprint", 1)),
                Topic("SI units",
                    P("The SI unit of force is the newton (N).", "What is the SI unit of force?", "Joule|Newton|Watt|Pascal", 1),
                    P("Energy is measured in joules (J) and power in watts (W).", "Power is measured in…", "Joules|Newtons|Watts|Metres", 2)),
                Topic("Safety and ethics",
                    P("Engineers have a duty to protect public health and safety.", "An engineer's first duty is to…", "Maximise profit at any cost|Protect public safety|Avoid documentation|Ignore standards", 1),
                    P("A factor of safety designs a structure to carry more than its expected load.", "Designing a bridge to hold more than its expected load uses a…", "Factor of safety|Discount rate|Break-even point|Tolerance stack", 0)),
                Topic("Technical drawing",
                    P("Orthographic projection shows an object in separate 2D views such as front, top and side.", "Orthographic projection shows…", "A 3D photo|Separate 2D views of an object|A coloured sketch|A circuit diagram", 1),
                    P("At a scale of 1:100, 1 cm on the drawing represents 100 cm (1 m) in reality.", "At 1:100, 5 cm on the drawing is…", "5 cm|50 cm|5 m|500 m", 2)));

            // ---------------------------------------------------------- Nursing
            yield return Course("NUR 101", "Human Anatomy I", "nursing", CourseKind.Core, "Dr. Afua Sarpong",
                Topic("Organisation of the body",
                    P("The levels of organisation are cells, tissues, organs, organ systems and the organism.", "Which level comes immediately after tissues?", "Cells|Organs|Organ systems|Organism", 1),
                    P("The cell is the basic structural and functional unit of life.", "The basic unit of life is the…", "Organ|Tissue|Cell|Organ system", 2)),
                Topic("The skeletal system",
                    P("The adult human skeleton has 206 bones.", "How many bones are in the adult human skeleton?", "106|206|306|186", 1),
                    P("The femur (thigh bone) is the longest bone in the body.", "The longest bone in the body is the…", "Humerus|Tibia|Femur|Radius", 2)),
                Topic("Circulation",
                    P("The heart has four chambers: two atria and two ventricles.", "How many chambers does the human heart have?", "2|3|4|6", 2),
                    P("Arteries carry blood away from the heart; veins carry blood back to it.", "Vessels that carry blood away from the heart are…", "Veins|Arteries|Lymph nodes|Nerves", 1)),
                Topic("Respiration",
                    P("Gas exchange in the lungs happens in tiny air sacs called alveoli.", "Where does gas exchange happen in the lungs?", "Bronchi|Alveoli|Trachea|Diaphragm", 1),
                    P("The diaphragm contracts to help draw air into the lungs.", "Which muscle contracts to help you breathe in?", "Biceps|Diaphragm|Deltoid|Quadriceps", 1)));

            yield return Course("NUR 103", "Foundations of Nursing", "nursing", CourseKind.Core, "Mrs. Mabel Dogbe, RN",
                Topic("Vital signs",
                    P("Vital signs include temperature, pulse, respiration rate and blood pressure.", "Which is a vital sign?", "Hair colour|Pulse|Height|Shoe size", 1),
                    P("Normal adult body temperature is about 36.5–37.5 °C.", "A normal adult body temperature is about…", "33 °C|37 °C|40 °C|42 °C", 1)),
                Topic("Infection prevention",
                    P("Hand hygiene is the most important single measure for preventing infections in healthcare.", "The most important measure for preventing infections is…", "Wearing perfume|Hand hygiene|Daily antibiotics|Avoiding water", 1),
                    P("Gloves do not replace hand hygiene; clean your hands before putting on and after removing gloves.", "Should you clean your hands after removing gloves?", "No, gloves kept them clean|Yes|Only if visibly dirty|Only at the end of a shift", 1)),
                Topic("Ethics in nursing",
                    P("Patient confidentiality means not sharing a patient's information without proper authority.", "Discussing a patient's diagnosis on social media breaches…", "Confidentiality|Hydration|Triage|Asepsis", 0),
                    P("Informed consent is a patient's agreement to care after understanding its risks and benefits.", "Informed consent is…", "Consent given without information|Agreement after understanding risks and benefits|A doctor's signature only|A hospital bill", 1)),
                Topic("The nursing process",
                    P("The nursing process is Assessment, Diagnosis, Planning, Implementation and Evaluation.", "What is the first step of the nursing process?", "Planning|Assessment|Evaluation|Implementation", 1),
                    P("Evaluation checks whether the patient's care goals were met.", "Checking whether care goals were met is…", "Assessment|Evaluation|Diagnosis|Admission", 1)));

            // --------------------------------------------- Communication Studies
            yield return Course("COM 101", "Introduction to Mass Communication", "communication_studies", CourseKind.Core, "Dr. Esinam Adzraku",
                Topic("Models of communication",
                    P("A basic communication model has a sender, a message, a channel and a receiver.", "The medium a message travels through is the…", "Sender|Channel|Receiver|Noise", 1),
                    P("Noise is anything that stops a message being received as intended.", "Anything that interferes with a message is called…", "Feedback|Noise|Encoding|A channel", 1)),
                Topic("Mass media",
                    P("Mass media such as radio, television, newspapers and the internet reach large audiences.", "Which is a mass medium?", "A private letter|Radio|A whispered secret|A diary", 1),
                    P("Radio is a major source of news in Ghana, with many FM stations broadcasting in Ghanaian languages.", "Why is radio especially powerful in Ghana?", "It is banned elsewhere|It reaches many people, including in local languages|It has no adverts|It only plays music", 1)),
                Topic("Functions of the media",
                    P("Media functions include informing, educating, entertaining and setting the agenda.", "Which is a function of the media?", "Informing|Taxing|Legislating|Policing", 0),
                    P("Agenda-setting means the media influence which issues the public thinks are important.", "Media shaping which issues the public sees as important is…", "Gatekeeping|Agenda-setting|Framing|Censorship", 1)),
                Topic("Media ethics",
                    P("Journalists should verify information before publishing it.", "Before publishing a story, a journalist should…", "Verify it|Exaggerate it|Hide errors|Publish first, check later", 0),
                    P("Gatekeeping is the process of deciding which stories get published or broadcast.", "Deciding which stories get published is…", "Gatekeeping|Agenda-setting|Encoding|Plagiarism", 0)));

            yield return Course("COM 103", "Writing for the Media", "communication_studies", CourseKind.Core, "Mr. Nii Armah Tetteh",
                Topic("News values",
                    P("News values include timeliness, proximity, prominence, impact and human interest.", "A story about something that happened today has…", "Proximity|Timeliness|Prominence|Conflict", 1),
                    P("Proximity means events close to the audience are more newsworthy to them.", "Events close to the audience matter more because of…", "Timeliness|Proximity|Novelty|Celebrity", 1)),
                Topic("The inverted pyramid",
                    P("The inverted pyramid puts the most important information first.", "In the inverted pyramid, the most important information comes…", "Last|First|In the middle|In a footnote", 1),
                    P("A news lead answers who, what, when, where, why and how.", "Which is one of the 5 Ws?", "Which|Where|Whether|Whose", 1)),
                Topic("Media writing style",
                    P("Media writing uses short sentences, the active voice and plain language.", "Which sentence is in the active voice?", "The ball was kicked by Kofi.|Kofi kicked the ball.|The ball had been kicked.|Kicking was done.", 1),
                    P("Headlines should be short, accurate and attention-grabbing.", "A good headline is…", "Long and detailed|Short and accurate|In the passive voice|Full of jargon", 1)),
                Topic("Attribution and quotes",
                    P("Attribution tells readers where information came from.", "'…, the Vice-Chancellor said' is an example of…", "Attribution|Speculation|Opinion|Editorialising", 0),
                    P("A direct quote uses a source's exact words inside quotation marks.", "A source's exact words in quotation marks are a…", "Paraphrase|Direct quote|Summary|Headline", 1)));

            // -------------------------------------------------------- Education
            yield return Course("EDU 101", "Foundations of Education", "education", CourseKind.Core, "Dr. Kwadwo Obeng",
                Topic("Purposes of education",
                    P("Education passes knowledge, skills and values from one generation to the next.", "A key purpose of education is…", "Only passing exams|Passing on knowledge, skills and values|Replacing families|Creating jobs only", 1),
                    P("Formal education happens in structured institutions; informal education happens through everyday life.", "Learning to cook from your grandmother is…", "Formal education|Informal education|Tertiary education|Distance education", 1)),
                Topic("Education in Ghana",
                    P("Ghana's basic education covers kindergarten, primary and JHS, followed by senior high school.", "What comes after JHS in Ghana?", "Primary school|Senior high school|Kindergarten|Nursery", 1),
                    P("Ghana's Free Senior High School policy began in 2017.", "In which year did Free SHS begin in Ghana?", "2007|2012|2017|2020", 2)),
                Topic("Teaching and assessment",
                    P("Learner-centred teaching puts students' needs and active participation at the centre.", "Learner-centred teaching means…", "The teacher talks and students copy|Students actively participate|There is no assessment|Only exams matter", 1),
                    P("Formative assessment checks progress during learning to guide teaching.", "A short class quiz used to adjust teaching is…", "Summative assessment|Formative assessment|An entrance exam|A certificate", 1)),
                Topic("The teacher's role",
                    P("A professional teacher plans lessons, manages the classroom and assesses learning.", "A professional teacher…", "Plans lessons and assesses learning|Only grades exams|Only disciplines students|Only writes textbooks", 0),
                    P("Inclusive education means all learners, including those with disabilities, learn together with support.", "Inclusive education means…", "Separate schools for every group|All learners learning together with support|Only gifted students|Online classes only", 1)));

            yield return Course("EDU 103", "Child Development", "education", CourseKind.Core, "Mrs. Lamisi Atampugre",
                Topic("Domains of development",
                    P("Development is studied in physical, cognitive, social and emotional domains.", "Learning to reason and solve problems is which domain?", "Physical|Cognitive|Emotional|Social", 1),
                    P("Development follows a general sequence, but each child develops at their own pace.", "Which is true of children's development?", "All develop at exactly the same pace|Each develops at their own pace|It stops at age 5|It is random", 1)),
                Topic("Piaget",
                    P("Jean Piaget described four stages of cognitive development.", "How many stages of cognitive development did Piaget describe?", "2|3|4|8", 2),
                    P("Object permanence — knowing objects exist when unseen — develops in the sensorimotor stage.", "Object permanence develops in which stage?", "Formal operational|Sensorimotor|Concrete operational|Preoperational", 1)),
                Topic("Vygotsky",
                    P("Vygotsky's zone of proximal development is what a learner can do with help but not yet alone.", "What a learner can do with help but not yet alone is the…", "Zone of proximal development|Object permanence|Conservation|Egocentrism", 0),
                    P("Scaffolding is temporary support that is reduced as the learner becomes independent.", "Temporary support reduced as a learner improves is…", "Scaffolding|Punishment|Testing|Streaming", 0)),
                Topic("Play and the lifespan",
                    P("Play supports children's social, physical and cognitive development.", "What does research say about play?", "It wastes learning time|It supports development|It only builds muscles|It is only for weekends", 1),
                    P("Erik Erikson proposed stages of psychosocial development across the lifespan.", "Who proposed stages of psychosocial development?", "Erikson|Newton|Keynes|Darwin", 0)));

            // ----------------------------------------------- Political Science
            yield return Course("POL 101", "Introduction to Political Science", "political_science", CourseKind.Core, "Dr. Abdul-Karim Seidu",
                Topic("The state",
                    P("A state has a population, a defined territory, a government and sovereignty.", "Which is an element of a state?", "A football team|Sovereignty|A currency symbol|A national dish", 1),
                    P("Sovereignty is supreme authority within a territory, free from outside control.", "Supreme authority within a territory is…", "Sovereignty|Federalism|Citizenship|Suffrage", 0)),
                Topic("Democracy",
                    P("In a democracy, citizens choose their leaders through free and fair elections.", "In a democracy, leaders are chosen by…", "Inheritance|Free and fair elections|Military decree|Lottery", 1),
                    P("Universal adult suffrage means all adult citizens have the right to vote.", "Universal adult suffrage means…", "Only landowners vote|All adult citizens can vote|Only men vote|Only graduates vote", 1)),
                Topic("Separation of powers",
                    P("Separation of powers divides government into legislative, executive and judicial branches.", "Which branch makes laws?", "Executive|Legislature|Judiciary|Military", 1),
                    P("The judiciary interprets laws and settles disputes.", "Which branch interprets laws?", "Legislature|Executive|Judiciary|Electoral commission", 2)),
                Topic("Systems and parties",
                    P("Federal systems share power between central and regional governments; unitary systems keep main power at the centre.", "In a federal system, power is…", "Held only by the military|Shared between central and regional governments|Held by one region|Held only by traditional rulers", 1),
                    P("Political parties organise people with shared ideas to contest elections.", "An organisation that contests elections to form a government is a…", "Political party|Pressure group|Civil service|Judiciary", 0)));

            yield return Course("POL 103", "Government of Ghana", "political_science", CourseKind.Core, "Mr. Yaw Ntiamoah",
                Topic("The 1992 Constitution",
                    P("Ghana's Fourth Republic began on 7 January 1993 under the 1992 Constitution.", "Ghana's current constitution dates from…", "1957|1960|1979|1992", 3),
                    P("The 1992 Constitution is the supreme law of Ghana.", "What is the supreme law of Ghana?", "Parliament's standing orders|The 1992 Constitution|Customary law|Presidential speeches", 1)),
                Topic("The Executive",
                    P("Ghana's President is elected for a four-year term and may serve at most two terms.", "How long is a presidential term in Ghana?", "3 years|4 years|5 years|6 years", 1),
                    P("The President is both head of state and head of government.", "Ghana's President is…", "Only head of state|Head of state and head of government|Only commander of the army|Speaker of Parliament", 1)),
                Topic("Parliament",
                    P("Ghana's Parliament is unicameral — it has one chamber.", "How many chambers does Ghana's Parliament have?", "One|Two|Three|Four", 0),
                    P("Members of Parliament represent constituencies and are elected every four years.", "How often are parliamentary elections held?", "Every 2 years|Every 4 years|Every 5 years|Every 7 years", 1)),
                Topic("Local government and elections",
                    P("Metropolitan, Municipal and District Assemblies (MMDAs) are Ghana's local government units.", "Local government in Ghana is run through…", "MMDAs|Senates|Counties|Provinces", 0),
                    P("The Electoral Commission conducts and supervises public elections in Ghana.", "Which body conducts elections in Ghana?", "Parliament|The Electoral Commission|The Supreme Court|The Police Service", 1)));

            // ------------------------------------------------------- Psychology
            yield return Course("PSY 101", "Introduction to Psychology", "psychology", CourseKind.Core, "Dr. Naa Adoley Lartey",
                Topic("What is psychology?",
                    P("Psychology is the scientific study of behaviour and mental processes.", "Psychology is…", "The study of rocks|The scientific study of behaviour and mental processes|The study of stars|The study of money", 1),
                    P("Psychologists form hypotheses and test them with evidence.", "A testable prediction is a…", "Theory|Hypothesis|Fact|Anecdote", 1)),
                Topic("The brain",
                    P("Neurons are cells that transmit information in the nervous system.", "Cells that transmit information in the nervous system are…", "Neurons|Red blood cells|Bone cells|Skin cells", 0),
                    P("The frontal lobe is involved in planning, decision-making and self-control.", "Planning and decision-making are linked mainly to the…", "Occipital lobe|Frontal lobe|Cerebellum|Spinal cord", 1)),
                Topic("Learning",
                    P("Classical conditioning (Pavlov) pairs a neutral stimulus with one that naturally causes a response.", "Pavlov's dogs salivating at a bell is an example of…", "Operant conditioning|Classical conditioning|Observational learning|Insight", 1),
                    P("Operant conditioning (Skinner) changes behaviour through reinforcement and punishment.", "Rewarding a behaviour so it happens more often is…", "Reinforcement|Extinction|Punishment|Habituation", 0)),
                Topic("Memory",
                    P("Memory involves encoding, storage and retrieval.", "Getting information back out of memory is…", "Encoding|Storage|Retrieval|Perception", 2),
                    P("Spacing study sessions over time improves long-term retention compared with cramming.", "Which study strategy usually improves long-term memory?", "Cramming the night before|Spaced practice|Studying with the TV on|Skipping sleep", 1)));

            yield return Course("PSY 103", "Social Psychology", "psychology", CourseKind.Core, "Mr. Senyo Adzoyi",
                Topic("Social influence",
                    P("Conformity is changing your behaviour or beliefs to match a group.", "Changing your opinion to match your friends is…", "Obedience|Conformity|Prejudice|Altruism", 1),
                    P("Asch's line experiments showed people often conform to a group's wrong answer.", "Whose line-length experiments studied conformity?", "Asch|Freud|Piaget|Skinner", 0)),
                Topic("Attitudes",
                    P("An attitude is an evaluation of a person, object or idea.", "A positive or negative evaluation of something is an…", "Attitude|Instinct|Reflex|Hormone", 0),
                    P("Cognitive dissonance is the discomfort of holding conflicting beliefs or actions.", "Discomfort from conflicting beliefs and actions is…", "Groupthink|Cognitive dissonance|Social loafing|The halo effect", 1)),
                Topic("Groups",
                    P("Social loafing is putting in less effort in a group than when working alone.", "A group member doing less because others will cover is…", "Social facilitation|Social loafing|Leadership|Conformity", 1),
                    P("Groupthink happens when the desire for agreement overrides careful decision-making.", "When wanting agreement overrides careful decisions, it is…", "Groupthink|Brainstorming|Mediation|Delegation", 0)),
                Topic("Helping others",
                    P("The bystander effect: people are less likely to help when many others are present.", "Fewer people help when a crowd is present. This is the…", "Bystander effect|Placebo effect|Halo effect|Mere-exposure effect", 0),
                    P("Altruism is helping others without expecting a reward.", "Helping with no expectation of reward is…", "Altruism|Reciprocity|Bribery|Conformity", 0)));
        }
    }
}
