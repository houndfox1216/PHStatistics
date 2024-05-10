<template>
  <div class="intro-y grid grid-cols-12 gap-6 mt-5">
    <div class="col-span-12 lg:col-span-6">
      <!-- BEGIN: Default -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Default</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <Stepper 
              v-bind="{
                nodes: [
                  { step: 1, title: 'Step 1', template: 'step1' },
                  { step: 2, title: 'Step 2', template: 'step2' },
                  { step: 3, title: 'Step 3', template: 'step3' },
                  { step: 4, title: 'Step 4', template: 'step4' },
                  { step: 5, title: 'Step 5', template: 'step5' },
                ],
              }"
            >
              <template #step1>
                <div>Step 1</div>
              </template>
              <template #step2>
                <div>Step 2</div>
              </template>
              <template #step3>
                <div>Step 3</div>
              </template>
              <template #step4>
                <div>Step 4</div>
              </template>
              <template #step5>
                <div>Step 5</div>
              </template>
            </Stepper>
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <Stepper 
    v-bind="{
      nodes: [
        { step: 1, title: 'Step 1', template: 'step1' },
        { step: 2, title: 'Step 2', template: 'step2' },
        { step: 3, title: 'Step 3', template: 'step3' },
        { step: 4, title: 'Step 4', template: 'step4' },
        { step: 5, title: 'Step 5', template: 'step5' },
      ],
    }"
  >
    <template #step1>
      <div>Step 1</div>
    </template>
    <template #step2>
      <div>Step 2</div>
    </template>
    <template #step3>
      <div>Step 3</div>
    </template>
    <template #step4>
      <div>Step 4</div>
    </template>
    <template #step5>
      <div>Step 5</div>
    </template>
  </Stepper>
</template>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Defalt -->
      <!-- BEGIN: Custom icon, control and finish content -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Custom icon, control and finish content</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <Stepper
              ref="lifeStepper"
              v-bind="{
                nodes: [
                  { step: 1, icon: 'baby', title: '嬰兒期', template: 'step1' },
                  { step: 2, icon: 'child', title: '幼兒期', template: 'step2' },
                  { step: 3, icon: 'person', title: '成年期', template: 'step3' },
                  { step: 4, icon: 'person-cane', title: '老年期', template: 'step4' },
                  { step: 5, icon: 'ghost', title: '輪迴期', template: 'step5' },
                ],
              }"
            >
              <template #icon="{ type, icon }">
                <FontAwesome class="stepper-icon" :icon="icon" />
              </template>
              <template #step1>
                嬰兒期(infancy)，個體從出生到3歲以前的時期。這個時期通常又分為兩個階段，即出生到1歲階段和1～3歲階段。有的研究者分別稱之為乳兒期和嬰兒期。              
              </template>
              <template #step2>
                從1歲開始至滿3歲稱為幼兒期，這一時期是兒童智力的發展非常迅速的時期，是孩子的特殊才能開始表現的時期，也是個性、品質開始形成的時期。幼兒期個性的形成是以後個性發展的重要基礎。為了培養孩子在德、智、體幾方面更順利、更健康地成長，以配合“四化”的需要，多出人才和早出人才，家長應特別注意幼兒期的教育。要想與寶寶之間更加合作點，還需要彼此之間建立良好的關係。父母不要總是嘮叨寶寶所犯的每一個小的錯誤，這樣會給寶寶年幼的自尊心帶來傷害。而且寶寶如若總是受到媽媽的批評，自信心都會大減。因此，媽媽們最好每天都給孩子正面的評價，誇讚寶寶，而不是大聲斥責。
              </template>
              <template #step3>
                成年期是25—60歲這個年齡段。按成年人的心理和生理特點，又可分為三個階段：即青年晚期(25—30歲)，成年前期(30—40歲)，成年中期(40—60歲)。在成年期，人的身體發育成熟，各種生理活動相對穩定。但隨著年齡的增長，人體各器官系統的功能開始向衰退過渡。特別是在成年中期以後，身體各種功能，如體力、精力、抗病力、呈下降趨勢並隨年齡的增加而日漸明顯。所以，在青年晚期和成年前期是人生的黃金時期，體力、精力旺盛，社會經驗和文化知識豐富，成為社會各項事業、生產和活動的基本力量，是家庭養老育兒的主要承擔者，一定要珍惜這段時期。
              </template>
              <template #step4>
                老年期(old age)，人生過程的最後階段。特點是身體各器官組織出現明顯的退行性變化，心理方面也發生相應改變，衰老現象逐漸明顯。由于各種變化包括衰老是循序漸進的，人生各時期很難截然劃分。而且衰老過程的個體差異很大，即使在一個人身上，各髒器的衰老進度也不是同步的。衰老與一般健康水準有關，不同時代、不同地區的人，衰老進度也不同。多數人的衰老變化在40歲左右逐漸發展，60歲左右開始顯著。
              </template>
              <template #step5>
                輪迴是宗教信仰的範疇，是死亡之後，靈魂取向的事情，和現實生活距離遙遠，「不知生，焉知死」，對輪迴無需付與太大的關懷。然而當人們遇到、聽到周遭的生死輪迴真實事件後，就不得不相信：死亡之後，生命有繼續流轉的必然性。
              </template>
              <template #controls="{node, nodes}">
                <div class="stepper-controls">
                  <button class="stepper-button"
                    action="continue"
                    @click="node.step !== nodes[nodes.length - 1].step ? lifeStepper.next() : lifeStepper.complete()"
                  >
                    結束{{node.title}}
                  </button>
                </div>
              </template>
              <template #finish-content>
                <div class="text-center rounded-lg border-2 border-primary p-5">
                  <div class="text-xl mb-5">
                    “來時無跡去無蹤，去與來時事一同。”<br/>
                    “何須更問浮生事，只此浮生在夢中。”
                  </div>
                  經歷了生老病死~ 重新審視一生後，按下 F5 進入下個人生
                </div>
              </template>
            </Stepper>
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <Stepper
    ref="lifeStepper"
    v-bind="{
      nodes: [
        { step: 1, icon: 'baby', title: '嬰兒期', template: 'step1' },
        { step: 2, icon: 'child', title: '幼兒期', template: 'step2' },
        { step: 3, icon: 'person', title: '成年期', template: 'step3' },
        { step: 4, icon: 'person-cane', title: '老年期', template: 'step4' },
        { step: 5, icon: 'ghost', title: '輪迴期', template: 'step5' },
      ],
    }"
  >
    <template #icon="{ type, icon }">
      <FontAwesome class="stepper-icon" :icon="icon" />
    </template>
    <template #step1>
      嬰兒期(infancy)，個體從出生到3歲以前的時期。這個時期通常又分為兩個階段，即出生到1歲階段和1～3歲階段。有的研究者分別稱之為乳兒期和嬰兒期。              
    </template>
    <template #step2>
      從1歲開始至滿3歲稱為幼兒期，這一時期是兒童智力的發展非常迅速的時期，是孩子的特殊才能開始表現的時期，也是個性、品質開始形成的時期。幼兒期個性的形成是以後個性發展的重要基礎。為了培養孩子在德、智、體幾方面更順利、更健康地成長，以配合“四化”的需要，多出人才和早出人才，家長應特別注意幼兒期的教育。要想與寶寶之間更加合作點，還需要彼此之間建立良好的關係。父母不要總是嘮叨寶寶所犯的每一個小的錯誤，這樣會給寶寶年幼的自尊心帶來傷害。而且寶寶如若總是受到媽媽的批評，自信心都會大減。因此，媽媽們最好每天都給孩子正面的評價，誇讚寶寶，而不是大聲斥責。
    </template>
    <template #step3>
      成年期是25—60歲這個年齡段。按成年人的心理和生理特點，又可分為三個階段：即青年晚期(25—30歲)，成年前期(30—40歲)，成年中期(40—60歲)。在成年期，人的身體發育成熟，各種生理活動相對穩定。但隨著年齡的增長，人體各器官系統的功能開始向衰退過渡。特別是在成年中期以後，身體各種功能，如體力、精力、抗病力、呈下降趨勢並隨年齡的增加而日漸明顯。所以，在青年晚期和成年前期是人生的黃金時期，體力、精力旺盛，社會經驗和文化知識豐富，成為社會各項事業、生產和活動的基本力量，是家庭養老育兒的主要承擔者，一定要珍惜這段時期。
    </template>
    <template #step4>
      老年期(old age)，人生過程的最後階段。特點是身體各器官組織出現明顯的退行性變化，心理方面也發生相應改變，衰老現象逐漸明顯。由于各種變化包括衰老是循序漸進的，人生各時期很難截然劃分。而且衰老過程的個體差異很大，即使在一個人身上，各髒器的衰老進度也不是同步的。衰老與一般健康水準有關，不同時代、不同地區的人，衰老進度也不同。多數人的衰老變化在40歲左右逐漸發展，60歲左右開始顯著。
    </template>
    <template #step5>
      輪迴是宗教信仰的範疇，是死亡之後，靈魂取向的事情，和現實生活距離遙遠，「不知生，焉知死」，對輪迴無需付與太大的關懷。然而當人們遇到、聽到周遭的生死輪迴真實事件後，就不得不相信：死亡之後，生命有繼續流轉的必然性。
    </template>
    <template #controls="{node, nodes}">
      <div class="stepper-controls">
        <button class="stepper-button"
          action="continue"
          @click="node.step !== nodes[nodes.length - 1].step ? lifeStepper.next() : lifeStepper.complete()"
        >
          結束\{\{node.title\}\}
        </button>
      </div>
    </template>
    <template #finish-content>
      <div class="text-center rounded-lg border-2 border-primary p-5">
        <div class="text-xl mb-5">
          “來時無跡去無蹤，去與來時事一同。”<br/>
          “何須更問浮生事，只此浮生在夢中。”
        </div>
        經歷了生老病死~ 重新審視一生後，按下 F5 進入下個人生
      </div>
    </template>
  </Stepper>
</template>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Custom icon, control and finish content -->
      <!-- BEGIN: Jumpable & Backable & Skipable & Resettable -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Jumpable & Backable & Skipable & Resettable</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <Stepper 
              v-bind="{
                nodes: [
                  {
                    step: 1,
                    title: 'What\'s your first name?',
                    data: personalInfo,
                    template: 'step1',
                    validate: (node) => { const result = node.data.firstName?.trim()?.length; if (result) return true; else this.$send('error', 'The first name is required'); },
                    reset: (node) => node.data.firstName = 'Anonymous',
                  },
                  {
                    step: 2,
                    title: 'What\'s your middle name?',
                    data: personalInfo,
                    template: 'step2',
                    skipable: true,
                    validate: (node) => { const result = node.data.middleName?.trim()?.length; if (result) return true; else this.$send('error', 'The middle name can be skipped.'); },
                    reset: (node) => node.data.middleName = '',
                  },
                  {
                    step: 3,
                    title: 'What\'s your last name?',
                    data: personalInfo,
                    template: 'step3',
                    skipable: true,
                    validate: (node) => { const result = node.data.lastName?.trim()?.length; if (result) return true; else this.$send('error', 'The last name can be skipped.'); },
                  },
                ],
                backable: true,
                jumpable: true,
              }"
            >
              <template #step1="{ node }">
                <label class="form-label">First name:</label>
                <input type="text" class="form-control" placeholder="Input text" v-model="node.data.firstName" />              
              </template>
              <template #step2="{ node }">
                <label class="form-label">Middle name:</label>
                <input type="text" class="form-control" placeholder="Input text" v-model="node.data.middleName" />              
              </template>
              <template #step3="{ node }">
                <label class="form-label">Last name:</label>
                <input type="text" class="form-control" placeholder="Input text" v-model="node.data.lastName" />              
              </template>
              <template #finish-content="{ nodes }">
                <div class="rounded border p-3">
                  First name: {{personalInfo.firstName}} <br/>
                  Middle name: {{personalInfo.middleName}} <br/>
                  Last name: {{personalInfo.lastName}}
                </div>
              </template>
            </Stepper>
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <Stepper 
    v-bind="{
      nodes: [
        {
          step: 1,
          title: 'What\'s your first name?',
          data: personalInfo,
          template: 'step1',
          validate: (node) => node.data.firstName?.trim()?.length,
          reset: (node) => node.data.firstName = 'Anonymous',
        },
        {
          step: 2,
          title: 'What\'s your middle name?',
          data: personalInfo,
          template: 'step2',
          skipable: true,
          validate: (node) => node.data.middleName?.trim()?.length,
          reset: (node) => node.data.middleName = '',
        },
        {
          step: 3,
          title: 'What\'s your last name?',
          data: personalInfo,
          template: 'step3',
          skipable: true,
          validate: (node) => node.data.lastName?.trim()?.length,
          reset: (node) => node.data.lastName = '',
        },
      ],
      backable: true,
      jumpable: true,
    }"
  >
    <template #step1="{ node }">
      <label class="form-label">First name:</label>
      <input type="text" class="form-control" placeholder="Input text" v-model="node.data.firstName" />              
    </template>
    <template #step2="{ node }">
      <label class="form-label">Middle name:</label>
      <input type="text" class="form-control" placeholder="Input text" v-model="node.data.middleName" />              
    </template>
    <template #step3="{ node }">
      <label class="form-label">Last name:</label>
      <input type="text" class="form-control" placeholder="Input text" v-model="node.data.lastName" />              
    </template>
    <template #finish-content="{ nodes }">
      <div class="rounded border p-3">
        First name: \{\{personalInfo.firstName\}\} <br/>
        Middle name: \{\{personalInfo.middleName\}\} <br/>
        Last name: \{\{personalInfo.lastName\}\}
      </div>
    </template>
  </Stepper>
</template>
<script setup>
const personalInfo = ref({ firstName: "Anonymous", middleName: "", lastName: "" });
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Jumpable & Backable & Skipable & Resettable -->
      <!-- BEGIN: Custom Icon & Hint & Validate -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Custom Icon & Hint & Validate</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <Stepper 
              v-bind="{
                nodes: [
                  {
                    step: 1,
                    icon: 'user',
                    title: '個人資料',
                    data: order.buyer,
                    template: 'step1',
                    hint: 'hint1',
                    hintPosition: 'left',
                    hintWidth: 200,
                    validate: (node) => validateBuyer(node.data),
                  },
                  {
                    step: 2,
                    icon: 'truck',
                    title: '運送方式',
                    data: order.recipient,
                    template: 'step2',
                    hint: 'hint2',
                    hintPosition: 'right',
                    hintWidth: 200,
                    validate: (node) => validateRecipient(node.data),
                  },
                  {
                    step: 3,
                    icon: 'credit-card',
                    title: '信用卡資料',
                    data: order.creditCard,
                    template: 'step3',
                    validate: (node) => validateCreditCard(node.data),
                  },
                  {
                    step: 4,
                    icon: 'check-double',
                    title: '確認訂單資訊',
                    data: order,
                    template: 'step4',
                  },
                ],
                backable: true,
                jumpable: true,
              }"
            >
              <template #icon="{ type, icon }">
                <FontAwesome class="stepper-icon" :icon="icon" />
              </template>
              <template #step1="{ node }">
                <label class="form-label">姓名</label>
                <input type="text" class="form-control" placeholder="請輸入姓名" v-model="node.data.name" />
                <label class="form-label mt-5">電話</label>
                <input type="text" class="form-control" placeholder="09XX-XXX-XXX" v-model="node.data.phone" />
                <label class="form-label mt-5">Email</label>
                <input type="text" class="form-control" placeholder="email@address.com" v-model="node.data.email" />
              </template>
              <template #hint1="{}">
                <div>
                  &nbsp;&nbsp;~ 左側選單示範 ~<br/><br/>
                  請提供您正確的連絡資訊。<br/>
                  電話號碼可輸入手機或市話號碼。<br/>
                  Email請勿使用hotmail及MSN郵件信箱。
                </div>
              </template>
              <template #step2="{ node }">
                <label class="form-label">收件人姓名</label>
                <input type="text" class="form-control" placeholder="請輸入姓名" v-model="node.data.name" />              
                <label class="form-label mt-5">收件人電話</label>
                <input type="text" class="form-control" placeholder="09XX-XXX-XXX" v-model="node.data.phone" />              
                <label class="form-label mt-5">收件人地址</label>
                <input type="text" class="form-control" placeholder="請輸入地址" v-model="node.data.address" />              
              </template>
              <template #hint2="{ node, nodes }">
                <div>
                  &nbsp;&nbsp;~ 右側選單示範 ~<br/><br/>
                  請提供正確的收件人資訊，以利貨運投遞。<br/>
                  電話號碼可輸入手機或市話號碼。<br/>
                  地址須包含5碼郵遞區號。
                </div>
              </template>
              <template #step3="{ node }">
                <label class="form-label">卡號</label>
                <input type="text" class="form-control" placeholder="請輸入13或16碼卡號" v-model="node.data.number" />              
                <label class="form-label mt-5">有效月年</label>
                <input type="text" class="form-control" placeholder="MM/YY" v-model="node.data.expiry" />              
                <label class="form-label mt-5">後三碼</label>
                <input type="text" class="form-control" placeholder="CVC" v-model="node.data.cvc" />
              </template>
              <template #step4="{ node }">
                <div>
                  <fieldset class="rounded-md border-2 p-3 flex">
                    <legend>購買人</legend>
                    <span>姓名: {{node.data.buyer.name}}</span>
                    <span class="ml-6">電話: {{node.data.buyer.phone}}</span>
                    <span class="ml-6">Email: {{node.data.buyer.email}}</span>
                  </fieldset>
                  <fieldset class="mt-3 rounded-md border-2 p-3 flex">
                    <legend>收件人</legend>
                    <span>姓名: {{node.data.recipient.name}}</span>
                    <span class="ml-6">電話: {{node.data.recipient.phone}}</span>
                    <span class="ml-6">地址: {{node.data.recipient.address}}</span>
                  </fieldset>
                  <fieldset class="mt-3 rounded-md border-2 p-3 flex">
                    <legend>信用卡</legend>
                    <span>卡號: {{node.data.creditCard.number}}</span>
                    <span class="ml-6">有效月年: {{node.data.creditCard.expiry}}</span>
                    <span class="ml-6">後三碼: {{node.data.creditCard.cvc}}</span>
                  </fieldset>
                </div>
              </template>
              <template #finish-content="{}">
                <div class="bg-primary/70 w-full rounded border text-center">~~ 訂單已成立 ~~</div>
                <fieldset class="mt-3 rounded-md border-2 p-3 flex">
                  <legend>購買人</legend>
                  <span>姓名: {{order.buyer.name}}</span>
                  <span class="ml-6">電話: {{order.buyer.phone}}</span>
                  <span class="ml-6">Email: {{order.buyer.email}}</span>
                </fieldset>
                <fieldset class="mt-3 rounded-md border-2 p-3 flex">
                  <legend>收件人</legend>
                  <span>姓名: {{order.recipient.name}}</span>
                  <span class="ml-6">電話: {{order.recipient.phone}}</span>
                  <span class="ml-6">地址: {{order.recipient.address}}</span>
                </fieldset>
                <fieldset class="mt-3 rounded-md border-2 p-3 flex">
                  <legend>信用卡</legend>
                  <span>卡號: {{order.creditCard.number}}</span>
                  <span class="ml-6">有效月年: {{order.creditCard.expiry}}</span>
                  <span class="ml-6">後三碼: {{order.creditCard.cvc}}</span>
                </fieldset>
              </template>
            </Stepper>
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <Stepper 
    v-bind="{
      nodes: [
        {
          step: 1,
          icon: 'user',
          title: '個人資料',
          data: order.buyer,
          template: 'step1',
          hint: 'hint1',
          hintPosition: 'left',
          hintWidth: 200,
          validate: (node) => validateBuyer(node.data),
        },
        {
          step: 2,
          icon: 'truck',
          title: '運送方式',
          data: order.recipient,
          template: 'step2',
          hint: 'hint2',
          hintPosition: 'right',
          hintWidth: 200,
          validate: (node) => validateRecipient(node.data),
        },
        {
          step: 3,
          icon: 'credit-card',
          title: '信用卡資料',
          data: order.creditCard,
          template: 'step3',
          validate: (node) => validateCreditCard(node.data),
        },
        {
          step: 4,
          icon: 'check-double',
          title: '確認訂單資訊',
          data: order,
          template: 'step4',
        },
      ],
      backable: true,
      jumpable: true,
    }"
  >
    <template #icon="{ type, icon }">
      <FontAwesome class="stepper-icon" :icon="icon" />
    </template>
    <template #step1="{ node }">
      <label class="form-label">姓名</label>
      <input type="text" class="form-control" placeholder="請輸入姓名" v-model="node.data.name" />
      <label class="form-label mt-5">電話</label>
      <input type="text" class="form-control" placeholder="09XX-XXX-XXX" v-model="node.data.phone" />
      <label class="form-label mt-5">Email</label>
      <input type="text" class="form-control" placeholder="email@address.com" v-model="node.data.email" />
    </template>
    <template #hint1="{}">
      <div>
        &nbsp;&nbsp;~ 左側選單示範 ~<br/><br/>
        請提供您正確的連絡資訊。<br/>
        電話號碼可輸入手機或市話號碼。<br/>
        Email請勿使用hotmail及MSN郵件信箱。
      </div>
    </template>
    <template #step2="{ node }">
      <label class="form-label">收件人姓名</label>
      <input type="text" class="form-control" placeholder="請輸入姓名" v-model="node.data.name" />              
      <label class="form-label mt-5">收件人電話</label>
      <input type="text" class="form-control" placeholder="09XX-XXX-XXX" v-model="node.data.phone" />              
      <label class="form-label mt-5">收件人地址</label>
      <input type="text" class="form-control" placeholder="請輸入地址" v-model="node.data.address" />              
    </template>
    <template #hint2="{ node, nodes }">
      <div>
        &nbsp;&nbsp;~ 右側選單示範 ~<br/><br/>
        請提供正確的收件人資訊，以利貨運投遞。<br/>
        電話號碼可輸入手機或市話號碼。<br/>
        地址須包含5碼郵遞區號。
      </div>
    </template>
    <template #step3="{ node }">
      <label class="form-label">卡號</label>
      <input type="text" class="form-control" placeholder="請輸入13或16碼卡號" v-model="node.data.number" />              
      <label class="form-label mt-5">有效月年</label>
      <input type="text" class="form-control" placeholder="MM/YY" v-model="node.data.expiry" />              
      <label class="form-label mt-5">後三碼</label>
      <input type="text" class="form-control" placeholder="CVC" v-model="node.data.cvc" />
    </template>
    <template #step4="{ node }">
      <div>
        <fieldset class="rounded-md border-2 p-3 flex">
          <legend>購買人</legend>
          <span>姓名: \{\{node.data.buyer.name\}\}</span>
          <span class="ml-6">電話: \{\{node.data.buyer.phone\}\}</span>
          <span class="ml-6">Email: \{\{node.data.buyer.email\}\}</span>
        </fieldset>
        <fieldset class="mt-3 rounded-md border-2 p-3 flex">
          <legend>收件人</legend>
          <span>姓名: \{\{node.data.recipient.name\}\}</span>
          <span class="ml-6">電話: \{\{node.data.recipient.phone\}\}</span>
          <span class="ml-6">地址: \{\{node.data.recipient.address\}\}</span>
        </fieldset>
        <fieldset class="mt-3 rounded-md border-2 p-3 flex">
          <legend>信用卡</legend>
          <span>卡號: \{\{node.data.creditCard.number\}\}</span>
          <span class="ml-6">有效月年: \{\{node.data.creditCard.expiry\}\}</span>
          <span class="ml-6">後三碼: \{\{node.data.creditCard.cvc\}\}</span>
        </fieldset>
      </div>
    </template>
    <template #finish-content="{}">
      <div class="bg-primary/70 w-full rounded border text-center">~~ 訂單已成立 ~~</div>
      <fieldset class="mt-3 rounded-md border-2 p-3 flex">
        <legend>購買人</legend>
        <span>姓名: \{\{order.buyer.name\}\}</span>
        <span class="ml-6">電話: \{\{order.buyer.phone\}\}</span>
        <span class="ml-6">Email: \{\{order.buyer.email\}\}</span>
      </fieldset>
      <fieldset class="mt-3 rounded-md border-2 p-3 flex">
        <legend>收件人</legend>
        <span>姓名: \{\{order.recipient.name\}\}</span>
        <span class="ml-6">電話: \{\{order.recipient.phone\}\}</span>
        <span class="ml-6">地址: \{\{order.recipient.address\}\}</span>
      </fieldset>
      <fieldset class="mt-3 rounded-md border-2 p-3 flex">
        <legend>信用卡</legend>
        <span>卡號: \{\{order.creditCard.number\}\}</span>
        <span class="ml-6">有效月年: \{\{order.creditCard.expiry\}\}</span>
        <span class="ml-6">後三碼: \{\{order.creditCard.cvc\}\}</span>
      </fieldset>
    </template>
  </Stepper>
</template>
<script setup>
const order = reactive({ 
  buyer: { name: "", phone: "", email: "" }, 
  recipient: { name: "", phone: "", address: "" }, 
  creditCard: { number: "", expiry: "", cvc: "" }, 
});
function validateBuyer(buyer) {
  if (buyer.name.trim().length === 0) {
    context.send("error", "請提供購買人姓名");
    return false;
  }
  if (context.utils.validator.validatePhoneNumber(buyer.phone)) {
    context.send("error", "購買人電話格式錯誤");
    return false;
  }
  if (context.utils.validator.validateEmail(buyer.email)) {
    context.send("error", "購買人Email格式錯誤");
    return false;
  }
  return true;
}
function validateRecipient(recipient) {
  if (recipient.name.trim().length === 0) {
    context.send("error", "請提供收件人姓名");
    return false;
  }
  if (context.utils.validator.validatePhoneNumber(recipient.phone)) {
    context.send("error", "收件人電話格式錯誤");
    return false;
  }
  return true;
}
function validateCreditCard(card) {
  if (!/^(?:4[0-9]{12}(?:[0-9]{3})?|[25][1-7][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})$/.test(card.number)) {
    context.send("error", "卡號格式錯誤");
    return false;
  }
  if (!/[01][0-9]\/?[0-9]{2}/.test(card.expiry)) {
    context.send("error", "有效日期格式錯誤");
    return false;
  }
  if (!/[0-9]{3}/.test(card.cvc)) {
    context.send("error", "後三碼格式錯誤");
    return false;
  }
  return true;
}
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Custom Icon & Hint & Validate -->
    </div>
    <div class="col-span-12 lg:col-span-6">
      <div class="intro-y col-span-12 lg:col-span-6">
        <!-- BEGIN: Props -->
        <div class="intro-y box">
          <div
            class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
          >
            <h2 class="font-medium text-base mr-auto">Props</h2>
          </div>
          <div class="p-5">
            <table class="table">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Name</th>
                  <th class="whitespace-nowrap">Types</th>
                  <th class="whitespace-nowrap">Default</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`nodes`</td>
                  <td class="align-top">IStepperNode[]</td>
                  <td class="align-top">Required</td>
                  <td class="align-top">
                    步驟節點
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`backable`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">true</td>
                  <td class="align-top">
                    允許返回上個步驟
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`jumpable`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">true</td>
                  <td class="align-top">
                    允許跳至之前步驟
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`width`</td>
                  <td class="align-top">Number | String</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    寬度。數值型態的單位為 pixel
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`height`</td>
                  <td class="align-top">Number | String</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    高度。數值型態的單位為 pixel
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <!-- END: Props -->
        <!-- BEGIN: Slots -->
        <div class="intro-y box mt-5">
          <div
            class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
          >
            <h2 class="font-medium text-base mr-auto">Slots</h2>
          </div>
          <div class="p-5">
            <table class="table mt-5">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-36">Name</th>
                  <th class="whitespace-nowrap">Parameters</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`icon`</td>
                  <td class="align-top">
                    type: string,<br/>
                    icon: string
                  </td>
                  <td class="align-top">
                    圖示的slot，可用此進行客製化。<br/>
                    type: 圖示的類型，目前一律為 step，表示用於步驟的圖示<br/>
                    icon: 圖示名稱
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`button-content`</td>
                  <td class="align-top">
                    action: string,<br/>
                    icon: string<br/>
                    node: IStepperNode,<br/>
                    nodes: IStepperNode[]
                  </td>
                  <td class="align-top">
                    按鈕內容的slot，可用此進行客製化。<br/>
                    action: 表示為何種行為的按鈕<br/>
                    node: IStepperNode,<br/>
                    nodes: IStepperNode[]
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`controls`</td>
                  <td class="align-top">
                    node: IStepperNode,<br/>
                    nodes: IStepperNode[]
                  </td>
                  <td class="align-top">
                    操作按鈕的slot，可用此進行客製化。<br/>
                    node: 當前步驟<br/>
                    nodes: 所有步驟
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`finish-content`</td>
                  <td class="align-top">
                    node: IStepperNode,<br/>
                    nodes: IStepperNode[]
                  </td>
                  <td class="align-top">
                    完成時的顯示內容，可用此進行客製化。<br/>
                    node: 當前步驟<br/>
                    nodes: 所有步驟
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <!-- END: Slots -->
        <!-- BEGIN: Events -->
        <div class="intro-y box mt-5">
          <div
            class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
          >
            <h2 class="font-medium text-base mr-auto">Events</h2>
          </div>
          <div class="p-5">
            <table class="table mt-5">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Name</th>
                  <th class="whitespace-nowrap">Parameters</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`jump`</td>
                  <td class="align-top">
                    {<br/>
                    &nbsp;&nbsp;from: IStepperNode,<br/>
                    &nbsp;&nbsp;to: IStepperNode,<br/>
                    &nbsp;&nbsp;nodes: IStepperNode[]<br/>
                    },<br/>
                    function
                  </td>
                  <td class="align-top">
                    當跳至之前步驟時觸法此事件<br/>
                    來源步驟<br/>
                    目的步驟<br/>
                    所有步驟<br/>
                    <br/>
                    Callback方法。允許時須執行
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`back`</td>
                  <td class="align-top">
                    {<br/>
                    &nbsp;&nbsp;from: IStepperNode,<br/>
                    &nbsp;&nbsp;to: IStepperNode,<br/>
                    &nbsp;&nbsp;nodes: IStepperNode[]<br/>
                    },<br/>
                    function
                  </td>
                  <td class="align-top">
                    當返回上個步驟時觸法此事件<br/>
                    來源步驟<br/>
                    目的步驟<br/>
                    所有步驟<br/>
                    <br/>
                    Callback方法。允許時須執行
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`skip`</td>
                  <td class="align-top">
                    {<br/>
                    &nbsp;&nbsp;from: IStepperNode,<br/>
                    &nbsp;&nbsp;to: IStepperNode,<br/>
                    &nbsp;&nbsp;nodes: IStepperNode[]<br/>
                    },<br/>
                    function
                  </td>
                  <td class="align-top">
                    當跳過當前步驟時觸法此事件<br/>
                    來源步驟<br/>
                    目的步驟<br/>
                    所有步驟<br/>
                    <br/>
                    Callback方法。允許時須執行
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`next`</td>
                  <td class="align-top">from: IStepperNode,<br/>to: IStepperNode,<br />nodes: IStepperNode[]</td>
                  <td class="align-top">
                    當完成當前步驟時觸法此事件<br/>
                    來源步驟<br/>
                    目的步驟<br/>
                    所有步驟<br/>
                    <br/>
                    Callback方法。允許時須執行
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`completre`</td>
                  <td class="align-top">
                    <br/>
                    IStepperNode[],<br/>
                    function
                  </td>
                  <td class="align-top">
                    當完成所有步驟時觸法此事件<br/>
                    所有步驟<br/>
                    Callback方法。允許時須執行
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <!-- END: Event -->
        <!-- BEGIN: IStepperNode -->
        <div class="intro-y box mt-5">
          <div
            class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
          >
            <h2 class="font-medium text-base mr-auto">IStepperNode</h2>
          </div>
          <div class="p-5">
            <table class="table">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Property</th>
                  <th class="whitespace-nowrap">Types</th>
                  <th class="whitespace-nowrap">Default</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`step`</td>
                  <td class="align-top">number</td>
                  <td class="align-top">Required</td>
                  <td class="align-top">
                    號碼
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`icon`</td>
                  <td class="align-top">string</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    圖示，如未提供則顯示編號
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`title`</td>
                  <td class="align-top">string</td>
                  <td class="align-top">Required</td>
                  <td class="align-top">
                    標題，可為純文字或是HTML
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`content`</td>
                  <td class="align-top">string</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    內容，可為純文字或是HTML。與 template 擇一使用
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`template`</td>
                  <td class="align-top">string</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    樣板名稱，須提供同名稱之樣板。與 content 擇一使用
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`hint`</td>
                  <td class="align-top">string</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    提示的樣板名稱
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`hintPosition`</td>
                  <td class="align-top">'left' | 'right'</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    提示的位置
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`hintWidth`</td>
                  <td class="align-top">number | string</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    提示的寬度
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`data`</td>
                  <td class="align-top">any</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    資料，供樣板使用
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`skipable`</td>
                  <td class="align-top">boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    是否可跳過此步驟(不驗證)
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`validate`</td>
                  <td class="align-top">(node: IStepperNode, nodes: IStepperNode[]): Promise&lt;boolean&gt;</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    驗證資料請求
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`reset`</td>
                  <td class="align-top">(node: IStepperNode, nodes: IStepperNode[]): Promise&lt;void&gt;</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    重置資料請求
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`status`</td>
                  <td class="align-top">'current' | 'skiped' | 'validated'</td>
                  <td class="align-top">undefined</td>
                  <td class="align-top">
                    狀態
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <!-- END: IStepperNode -->
      </div>
    </div>
  </div>
</template>

<script setup>
import "@cloudfun/stepper/dist/stepper.scss";

import context, { reactive, ref } from "@cloudfun/core";
import Stepper from "@cloudfun/stepper";

const lifeStepper = ref();
const personalInfo = ref({ firstName: "Anonymous", middleName: "", lastName: "" });

const order = reactive({ 
  buyer: { name: "", phone: "", email: "" }, 
  recipient: { name: "", phone: "", address: "" }, 
  creditCard: { number: "", expiry: "", cvc: "" }, 
});

function validateBuyer(buyer) {
  if (buyer.name.trim().length === 0) {
    context.send("error", "請提供購買人姓名");
    return false;
  }
  if (context.utils.validator.validatePhoneNumber(buyer.phone)) {
    context.send("error", "購買人電話格式錯誤");
    return false;
  }
  if (context.utils.validator.validateEmail(buyer.email)) {
    context.send("error", "購買人Email格式錯誤");
    return false;
  }
  return true;
}

function validateRecipient(recipient) {
  if (recipient.name.trim().length === 0) {
    context.send("error", "請提供收件人姓名");
    return false;
  }
  if (context.utils.validator.validatePhoneNumber(recipient.phone)) {
    context.send("error", "收件人電話格式錯誤");
    return false;
  }
  return true;
}

function validateCreditCard(card) {
  console.log('card: ', card.number);
  if (!/^(?:4[0-9]{12}(?:[0-9]{3})?|[25][1-7][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})$/.test(card.number)) {
    context.send("error", "卡號格式錯誤");
    return false;
  }
  if (!/[01][0-9]\/?[0-9]{2}/.test(card.expiry)) {
    context.send("error", "有效日期格式錯誤");
    return false;
  }
  if (!/[0-9]{3}/.test(card.cvc)) {
    context.send("error", "後三碼格式錯誤");
    return false;
  }
  return true;
}
</script>